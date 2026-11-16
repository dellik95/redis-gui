using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Tests.Redis.DataTransfer;

[TestFixture]
public class RedisSnapshotIntegrationTests
{
	[Test]
	public async Task RestoreOverwrite_WorksInRedisCluster()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_CLUSTER");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_CLUSTER to run the Redis Cluster integration check.");
		using var mux = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(mux));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Cluster test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisSnapshotService(manager.Object, NullLogger<RedisSnapshotService>.Instance);
		var prefix = "redisgui-cluster-transfer:" + Guid.NewGuid().ToString("N") + ":";
		var db = mux.GetDatabase(0);
		byte[] source = Encoding.UTF8.GetBytes(prefix + "{source}");
		byte[] destination = Enumerable.Range(0, 100).Select(i => Encoding.UTF8.GetBytes(prefix + "{destination-" + i + "}"))
			.First(key => !Equals(db.IdentifyEndpoint((RedisKey)source), db.IdentifyEndpoint((RedisKey)key)));
		try
		{
			await db.StringSetAsync((RedisKey)source, "source-value");
			await db.ListRightPushAsync((RedisKey)destination, "old");
			var snapshot = await service.CaptureAsync(connection, source, 0);
			Assert.That(snapshot.IsSuccess, Is.True, snapshot.IsFailure ? snapshot.Error.Message : "");
			var restored = await service.RestoreAsync(connection, destination, snapshot.Value.Payload, null, true, 0);
			Assert.That(restored.IsSuccess && restored.Value, Is.True, restored.IsFailure ? restored.Error.Message : "");
			Assert.That((string?)await db.StringGetAsync((RedisKey)destination), Is.EqualTo("source-value"));
		}
		finally
		{
			await db.KeyDeleteAsync((RedisKey)source);
			await db.KeyDeleteAsync((RedisKey)destination);
		}
	}

	[Test]
	public async Task CaptureAndRestore_PreservesBinaryKeysValuesAndStreams()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var mux = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(mux));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Snapshot test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(1), ConnectionOwnerId.None());
		var service = new RedisSnapshotService(manager.Object, NullLogger<RedisSnapshotService>.Instance);
		var prefix = Encoding.UTF8.GetBytes("redisgui-snapshot:" + Guid.NewGuid().ToString("N") + ":");
		byte[] source = [.. prefix, 0xff, 1];
		byte[] destination = [.. prefix, 0xfe, 2];
		byte[] stream = [.. prefix, 3];
		byte[] restoredStream = [.. prefix, 4];
		var db = mux.GetDatabase(1);
		try
		{
			await db.StringSetAsync((RedisKey)source, (RedisValue)new byte[] { 0, 0xff, 3 }, TimeSpan.FromMinutes(5));
			var scanner = new RedisKeyService(manager.Object, NullLogger<RedisKeyService>.Instance);
			string? cursor = null;
			bool foundBinaryKey = false;
			do
			{
				var page = await scanner.ScanKeysAsync(connection, Encoding.UTF8.GetString(prefix) + "*", 1, cursor, 100);
				Assert.That(page.IsSuccess, Is.True, page.IsFailure ? page.Error.Message : "");
				foundBinaryKey |= page.Value.Keys.Any(item => item.RawKey is not null && item.RawKey.SequenceEqual(source));
				cursor = page.Value.NextCursor;
			} while (cursor is not null);
			Assert.That(foundBinaryKey, Is.True);
			var capture = await service.CaptureAsync(connection, source, 1);
			Assert.That(capture.IsSuccess, Is.True, capture.IsFailure ? capture.Error.Message : "");
			Assert.That(capture.Value.TtlMilliseconds, Is.GreaterThan(0));
			var restored = await service.RestoreAsync(connection, destination, capture.Value.Payload,
				TimeSpan.FromMilliseconds(capture.Value.TtlMilliseconds!.Value), false, 1);
			Assert.That(restored.IsSuccess && restored.Value, Is.True);
			Assert.That((byte[])(await db.StringGetAsync((RedisKey)destination))!, Is.EqualTo(new byte[] { 0, 0xff, 3 }));
			var collision = await service.RestoreAsync(connection, destination, capture.Value.Payload, null, false, 1);
			Assert.That(collision.IsSuccess && !collision.Value, Is.True);
			var replaced = await service.RestoreAsync(connection, destination, capture.Value.Payload, null, true, 1);
			Assert.That(replaced.IsSuccess && replaced.Value, Is.True, replaced.IsFailure ? replaced.Error.Message : "");
			var legacyName = Encoding.UTF8.GetString(prefix) + "legacy";
			await db.ListRightPushAsync(legacyName, "existing");
			try
			{
				var legacyResult = await service.SetLegacyStringAsync(connection, legacyName, "replacement", null, false, 1);
				Assert.That(legacyResult.IsSuccess && !legacyResult.Value, Is.True);
				Assert.That(await db.KeyTypeAsync(legacyName), Is.EqualTo(RedisType.List));
			}
			finally { await db.KeyDeleteAsync(legacyName); }

			for (int i = 0; i < 120; i++) await db.StreamAddAsync((RedisKey)stream, "value", i);
			var streamSnapshot = await service.CaptureAsync(connection, stream, 1);
			Assert.That(streamSnapshot.IsSuccess, Is.True, streamSnapshot.IsFailure ? streamSnapshot.Error.Message : "");
			var restoredResult = await service.RestoreAsync(connection, restoredStream, streamSnapshot.Value.Payload, null, false, 1);
			Assert.That(restoredResult.IsSuccess && restoredResult.Value, Is.True);
			Assert.That(await db.StreamLengthAsync((RedisKey)restoredStream), Is.EqualTo(120));
		}
		finally
		{
			await db.KeyDeleteAsync((RedisKey)source);
			await db.KeyDeleteAsync((RedisKey)destination);
			await db.KeyDeleteAsync((RedisKey)stream);
			await db.KeyDeleteAsync((RedisKey)restoredStream);
		}
	}

	[Test]
	public async Task ExportSession_DeduplicatesBinaryKeysAndKeepsCompletedOutputReadable()
	{
		var factory = new RedisExportSessionFactory();
		System.IO.Stream output;
		await using (var session = await factory.CreateAsync(CancellationToken.None))
		{
			Assert.That(await session.AddKeyAsync([0xff, 1], CancellationToken.None), Is.True);
			Assert.That(await session.AddKeyAsync([0xff, 1], CancellationToken.None), Is.False);
			Assert.That(await session.AddKeyAsync([0xfe, 1], CancellationToken.None), Is.True);
			await session.Output.WriteAsync(new byte[] { 1, 2, 3 });
			session.Output.Position = 0;
			output = session.DetachOutput();
		}
		await using (output) Assert.That(output.ReadByte(), Is.EqualTo(1));
	}
}

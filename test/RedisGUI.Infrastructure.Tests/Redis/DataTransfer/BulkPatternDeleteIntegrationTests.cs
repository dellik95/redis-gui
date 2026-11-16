using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Infrastructure.Redis.TypeHandlers;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Tests.Redis.DataTransfer;

[TestFixture]
public class BulkPatternDeleteIntegrationTests
{
	[Test]
	public async Task BulkTtl_ReportsUpdatedAndUnchangedKeys()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var mux = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(mux));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Bulk TTL test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisValueService(manager.Object, Mock.Of<IRedisKeyService>(), Mock.Of<IRedisTypeHandlerFactory>(),
			Mock.Of<IRedisLoadTestService>(), Mock.Of<IRedisSeedDataService>(), Mock.Of<IRedisCliService>(),
			Mock.Of<IRedisDiagnosticsService>(), NullLogger<RedisValueService>.Instance);
		var key = "redisgui-bulk-ttl-test:" + Guid.NewGuid().ToString("N");
		var db = mux.GetDatabase(1);
		try
		{
			await db.StringSetAsync(key, "value", TimeSpan.FromMinutes(5));
			var result = await service.BulkUpdateTtlAsync(connection, [key, key + ":missing"], null, 1);
			Assert.That(result.IsSuccess, Is.True);
			Assert.Multiple(() =>
			{
				Assert.That(result.Value.UpdatedCount, Is.EqualTo(1));
				Assert.That(result.Value.UnchangedCount, Is.EqualTo(1));
				Assert.That(result.Value.FailedKeys, Is.Empty);
			});
			Assert.That(await db.KeyTimeToLiveAsync(key), Is.Null);
		}
		finally { await db.KeyDeleteAsync(key); }
	}

	[Test]
	public async Task DeletePattern_UsesSelectedDatabaseAndDeletesBinaryKeysAcrossPages()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var mux = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(mux));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Bulk test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisKeyService(manager.Object, NullLogger<RedisKeyService>.Instance);
		var prefix = "redisgui-bulk-test:" + Guid.NewGuid().ToString("N") + ":";
		var rawKeys = Enumerable.Range(0, 150).Select(i => Encoding.UTF8.GetBytes(prefix + i)).ToList();
		rawKeys.Add([.. Encoding.UTF8.GetBytes(prefix), 0xff]);
		var source = mux.GetDatabase(1);
		var other = mux.GetDatabase(2);
		try
		{
			foreach (var key in rawKeys) await source.StringSetAsync((RedisKey)key, "value");
			await other.StringSetAsync((RedisKey)rawKeys[0], "other database");
			var result = await service.DeleteKeysByPatternAsync(connection, prefix + "*", CancellationToken.None, 1);
			Assert.That(result.IsSuccess, Is.True, result.IsFailure ? result.Error.Message : "");
			Assert.That(result.Value, Is.EqualTo(rawKeys.Count));
			foreach (var key in rawKeys) Assert.That(await source.KeyExistsAsync((RedisKey)key), Is.False);
			Assert.That(await other.KeyExistsAsync((RedisKey)rawKeys[0]), Is.True);
		}
		finally
		{
			foreach (var key in rawKeys) await source.KeyDeleteAsync((RedisKey)key);
			await other.KeyDeleteAsync((RedisKey)rawKeys[0]);
		}
	}
}

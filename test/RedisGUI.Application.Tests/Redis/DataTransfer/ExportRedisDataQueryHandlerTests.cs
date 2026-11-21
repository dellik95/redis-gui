using FluentResults;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Queries.ExportRedisData;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Queries;

[TestFixture]
public class ExportRedisDataQueryHandlerTests
{
	private RedisConnection connection = null!;
	private Mock<IRedisConnectionRepository> repository = null!;
	private Mock<IConnectionAccessService> access = null!;
	private Mock<IRedisKeyService> keys = null!;
	private Mock<IRedisSnapshotService> snapshots = null!;
	private TestSession session = null!;
	private ExportRedisDataQueryHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(connection));
		access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		keys = new Mock<IRedisKeyService>();
		snapshots = new Mock<IRedisSnapshotService>();
		session = new TestSession();
		var factory = new Mock<IExportSessionFactory>();
		factory.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(session);
		handler = new ExportRedisDataQueryHandler(repository.Object, access.Object, keys.Object, snapshots.Object, factory.Object);
	}

	[TearDown]
	public async Task TearDown() => await session.DisposeAsync();

	[Test]
	public async Task Handle_ExportsEveryPageAndDeduplicatesBinaryKeysInSelectedDatabase()
	{
		var first = Enumerable.Range(0, 100).Select(i => Item(i)).ToArray();
		var second = Enumerable.Range(99, 51).Select(i => Item(i)).ToArray();
		keys.Setup(x => x.ScanKeysAsync(connection, "user:*", 2, null, 100, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(new RedisKeyScanPage(first, "next")));
		keys.Setup(x => x.ScanKeysAsync(connection, "user:*", 2, "next", 100, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(new RedisKeyScanPage(second, null)));
		snapshots.Setup(x => x.CaptureAsync(connection, It.IsAny<byte[]>(), 2, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(new RedisKeySnapshot([0, 255, 1], "stream", 12345)));
		var result = await handler.Handle(new ExportRedisDataQuery(connection.Id, "user:*", ExportFormat.Json, 2), CancellationToken.None);
		Assert.That(result.IsSuccess, Is.True);
		await using var output = result.Value.Data;
		using var json = await JsonDocument.ParseAsync(output);
		Assert.That(json.RootElement.GetArrayLength(), Is.EqualTo(150));
		Assert.That(json.RootElement[0].GetProperty("KeyBase64").GetString(), Is.EqualTo(Convert.ToBase64String(first[0].RawKey!)));
		Assert.That(json.RootElement[0].GetProperty("DumpBase64").GetString(), Is.EqualTo(Convert.ToBase64String([0, 255, 1])));
		Assert.That(json.RootElement[0].GetProperty("TtlMilliseconds").GetInt64(), Is.EqualTo(12345));
		snapshots.Verify(x => x.CaptureAsync(connection, It.IsAny<byte[]>(), 2, It.IsAny<CancellationToken>()), Times.Exactly(150));
	}

	[Test]
	public async Task Handle_ReadFailure_DoesNotReturnPartialOutput()
	{
		keys.Setup(x => x.ScanKeysAsync(connection, "*", 0, null, 100, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(new RedisKeyScanPage([Item(1)], null)));
		snapshots.Setup(x => x.CaptureAsync(connection, It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Fail<RedisKeySnapshot>(new DomainError("Redis.KeyNotFound", "disappeared")));
		var result = await handler.Handle(new ExportRedisDataQuery(connection.Id), CancellationToken.None);
		Assert.That(result.GetDomainError().Code, Is.EqualTo("Export.KeyReadFailed"));
		Assert.That(session.Disposed, Is.True);
		Assert.That(session.Detached, Is.False);
	}

	[Test]
	public async Task Handle_ScannedKeyDisappears_DoesNotReturnPartialOutput()
	{
		keys.Setup(x => x.ScanKeysAsync(connection, "*", 0, null, 100, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(new RedisKeyScanPage([], null, MissingCount: 1)));
		var result = await handler.Handle(new ExportRedisDataQuery(connection.Id), CancellationToken.None);
		Assert.That(result.GetDomainError().Code, Is.EqualTo("Export.KeyReadFailed"));
		Assert.That(session.Detached, Is.False);
	}

	[Test]
	public async Task Handle_CsvFormat_UsesBinarySafeFields()
	{
		keys.Setup(x => x.ScanKeysAsync(connection, "*", 0, null, 100, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(new RedisKeyScanPage([Item(2)], null)));
		snapshots.Setup(x => x.CaptureAsync(connection, It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(new RedisKeySnapshot([0, 255], "string", null)));
		var result = await handler.Handle(new ExportRedisDataQuery(connection.Id, Format: ExportFormat.Csv), CancellationToken.None);
		Assert.That(result.IsSuccess, Is.True);
		await using var output = result.Value.Data;
		using var reader = new StreamReader(output);
		var text = await reader.ReadToEndAsync();
		Assert.That(text, Does.StartWith("KeyBase64,DumpBase64,Type,TtlMilliseconds"));
		Assert.That(text, Does.Contain(Convert.ToBase64String([0, 255])));
	}

	private static RedisKeyScanItem Item(int index) => new($"key-{index}", "String", null, [0xff, (byte)(index / 256), (byte)index]);

	private sealed class TestSession : IExportSession
	{
		private readonly HashSet<string> seen = [];
		private readonly MemoryStream output = new();
		public bool Disposed { get; private set; }
		public bool Detached { get; private set; }
		public Stream Output => output;
		public Task<bool> AddKeyAsync(byte[] key, CancellationToken cancellationToken) => Task.FromResult(seen.Add(Convert.ToBase64String(key)));
		public Stream DetachOutput() { Detached = true; return output; }
		public ValueTask DisposeAsync() { Disposed = true; if (!Detached) output.Dispose(); return ValueTask.CompletedTask; }
	}
}

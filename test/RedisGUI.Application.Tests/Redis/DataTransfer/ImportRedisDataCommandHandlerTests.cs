using FluentResults;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Application.Redis.Commands.ImportRedisData;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class ImportRedisDataCommandHandlerTests
{
	private RedisConnection connection = null!;
	private Mock<IRedisConnectionRepository> repository = null!;
	private Mock<IRedisSnapshotService> snapshots = null!;
	private Mock<IConnectionAccessService> access = null!;
	private ImportRedisDataCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(connection));
		snapshots = new Mock<IRedisSnapshotService>();
		access = new Mock<IConnectionAccessService>();
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		handler = new ImportRedisDataCommandHandler(repository.Object, snapshots.Object, access.Object);
	}

	[Test]
	public async Task Handle_JsonSnapshot_PreservesBinaryKeyPayloadTtlAndDatabase()
	{
		byte[] key = [0, 255, 2];
		byte[] dump = [0, 3, 255];
		var json = JsonSerializer.Serialize(new[] { new RedisImportItem { KeyBase64 = Convert.ToBase64String(key), DumpBase64 = Convert.ToBase64String(dump), Type = "stream", TtlMilliseconds = 5000 } });
		snapshots.Setup(x => x.RestoreAsync(connection, It.Is<byte[]>(v => v[1] == 255), It.Is<byte[]>(v => v[2] == 255),
			TimeSpan.FromSeconds(5), false, 2, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(true));
		var result = await handler.Handle(new ImportRedisDataCommand(connection.Id, json, ImportFormat.Json, false, 2), CancellationToken.None);
		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value.ImportedCount, Is.EqualTo(1));
		snapshots.VerifyAll();
	}

	[Test]
	public async Task Handle_CsvSnapshot_ReportsCollisionAndFailure()
	{
		var csv = "KeyBase64,DumpBase64,Type,TtlMilliseconds\r\n\"/w==\",\"AA==\",\"string\",\"\"\r\n\"bad!\",\"AA==\",\"string\",\"\"\r\n";
		snapshots.Setup(x => x.RestoreAsync(connection, It.IsAny<byte[]>(), It.IsAny<byte[]>(), null, false, 0, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(false));
		var result = await handler.Handle(new ImportRedisDataCommand(connection.Id, csv, ImportFormat.Csv), CancellationToken.None);
		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value.SkippedCount, Is.EqualTo(1));
		Assert.That(result.Value.FailedCount, Is.EqualTo(1));
	}

	[Test]
	public async Task Handle_LegacyOnlyImportsStringsAndUsesAtomicSkip()
	{
		var items = new[] { new RedisImportItem { Key = "existing", Value = "old", Type = "String" }, new RedisImportItem { Key = "list", Value = "[]", Type = "List" } };
		snapshots.Setup(x => x.SetLegacyStringAsync(connection, "existing", "old", null, false, 0, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(false));
		var result = await handler.Handle(new ImportRedisDataCommand(connection.Id, JsonSerializer.Serialize(items), ImportFormat.Json), CancellationToken.None);
		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value.SkippedCount, Is.EqualTo(1));
		Assert.That(result.Value.FailedKeys, Does.Contain("list"));
	}

	[Test]
	public async Task Handle_CancelBetweenKeys_ReturnsConfirmedPartialResult()
	{
		using var cancellation = new CancellationTokenSource();
		var items = new[] { new RedisImportItem { Key = "a", Value = "1" }, new RedisImportItem { Key = "b", Value = "2" } };
		snapshots.Setup(x => x.SetLegacyStringAsync(connection, It.IsAny<string>(), It.IsAny<string>(), null, false, 0, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(true));
		var command = new ImportRedisDataCommand(connection.Id, JsonSerializer.Serialize(items), ImportFormat.Json,
			OnProgress: _ => { cancellation.Cancel(); return Task.CompletedTask; }, OperationCancellation: cancellation.Token);
		var logs = new Mock<IAuditLogRepository>();
		var pipeline = new AuditingBehavior<ImportRedisDataCommand, Result<ImportRedisDataResponse>>(
			logs.Object, Mock.Of<IUnitOfWork>(), Mock.Of<ICurrentUserService>());
		var result = await pipeline.Handle(command, () => handler.Handle(command, CancellationToken.None), CancellationToken.None);
		Assert.That(result.Value.ImportedCount, Is.EqualTo(1));
		Assert.That(result.Value.Cancelled, Is.True);
		Assert.That(result.Value.MayHaveUnconfirmedChanges, Is.False);
		logs.Verify(x => x.Add(It.Is<AuditLog>(log => log.Metadata.NewValue!.Contains("cancelled: True"))), Times.Once);
		snapshots.Verify(x => x.SetLegacyStringAsync(connection, It.IsAny<string>(), It.IsAny<string>(), null, false, 0, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_CancelDuringRedisCall_ReportsUnconfirmedChanges()
	{
		using var cancellation = new CancellationTokenSource();
		snapshots.Setup(x => x.SetLegacyStringAsync(connection, "a", "1", null, false, 0, It.IsAny<CancellationToken>()))
			.Returns(() => { cancellation.Cancel(); throw new OperationCanceledException(cancellation.Token); });
		var json = JsonSerializer.Serialize(new[] { new RedisImportItem { Key = "a", Value = "1" } });
		var result = await handler.Handle(new ImportRedisDataCommand(connection.Id, json, ImportFormat.Json, OperationCancellation: cancellation.Token), CancellationToken.None);
		Assert.That(result.Value.Cancelled, Is.True);
		Assert.That(result.Value.MayHaveUnconfirmedChanges, Is.True);
	}

	[Test]
	public async Task Handle_RejectsReadOnlyConnectionBeforeRestoring()
	{
		connection.SetReadOnly(true);
		var result = await handler.Handle(new ImportRedisDataCommand(connection.Id, "[]", ImportFormat.Json), CancellationToken.None);
		Assert.That(result.GetDomainError().Code, Is.EqualTo("Connection.ReadOnlyMode"));
		snapshots.VerifyNoOtherCalls();
	}
}

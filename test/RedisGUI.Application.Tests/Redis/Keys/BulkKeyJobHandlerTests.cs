using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Redis.Commands.BulkOperations;
using RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Keys;

[TestFixture]
public class BulkKeyJobHandlerTests
{
	[Test]
	public async Task Cancellation_StopsBetweenBatchesAndRetainsPartialFailures()
	{
		var mediator = new Mock<IMediator>();
		mediator.Setup(x => x.Send(It.IsAny<DeleteMultipleKeysCommand>(), CancellationToken.None))
			.ReturnsAsync(Result.Success(new DeleteMultipleKeysResponse(99, 100, ["failed"])));
		var handler = new BulkKeyJobHandler(Mock.Of<IRedisConnectionRepository>(), Mock.Of<IConnectionAccessService>(), mediator.Object);
		var parameters = JsonSerializer.SerializeToElement(new { operation = "delete", database = 2, keys = Enumerable.Range(0, 150).Select(i => $"key-{i}").ToArray() });
		using var cancellation = new CancellationTokenSource();
		var result = await handler.ExecuteAsync(Guid.NewGuid(), parameters, (_, _) => { cancellation.Cancel(); return Task.CompletedTask; }, cancellation.Token);
		var summary = (BulkKeyJobResult)result.Value!;
		Assert.That(summary.Processed, Is.EqualTo(100));
		Assert.That(summary.Changed, Is.EqualTo(99));
		Assert.That(summary.Failed, Is.EqualTo(1));
		Assert.That(summary.Cancelled, Is.True);
		mediator.Verify(x => x.Send(It.IsAny<DeleteMultipleKeysCommand>(), CancellationToken.None), Times.Once);
	}

	[Test]
	public async Task FailureAfterBatch_ReturnsPartialResultAndUnconfirmedFlag()
	{
		var mediator = new Mock<IMediator>();
		mediator.SetupSequence(x => x.Send(It.IsAny<DeleteMultipleKeysCommand>(), CancellationToken.None))
			.ReturnsAsync(Result.Success(new DeleteMultipleKeysResponse(100, 100, [])))
			.ReturnsAsync(Result.Failure<DeleteMultipleKeysResponse>(new Error("Redis.Error", "Batch failed.")));
		var handler = new BulkKeyJobHandler(Mock.Of<IRedisConnectionRepository>(), Mock.Of<IConnectionAccessService>(), mediator.Object);
		var parameters = JsonSerializer.SerializeToElement(new { operation = "delete", database = 0, keys = Enumerable.Range(0, 150).Select(i => $"key-{i}").ToArray() });
		var result = await handler.ExecuteAsync(Guid.NewGuid(), parameters, (_, _) => Task.CompletedTask, CancellationToken.None);
		var outcome = (BackgroundJobOutcome)result.Value!;
		var summary = (BulkKeyJobResult)outcome.Result!;
		Assert.That(summary.Changed, Is.EqualTo(100));
		Assert.That(summary.Processed, Is.EqualTo(100));
		Assert.That(summary.MayHaveUnconfirmedChanges, Is.True);
		Assert.That(outcome.Error, Is.EqualTo("Batch failed."));
	}

	[Test]
	public async Task Validate_RejectsUnauthorizedAndReadOnlyConnections()
	{
		var connection = RedisConnection.Create(new ConnectionName("Test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None()).Value;
		var repository = new Mock<IRedisConnectionRepository>();
		repository.Setup(x => x.GetConnectionByIdAsync(connection.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(connection));
		var access = new Mock<IConnectionAccessService>();
		var handler = new BulkKeyJobHandler(repository.Object, access.Object, Mock.Of<IMediator>());
		var parameters = JsonSerializer.SerializeToElement(new { operation = "delete", database = 0, keys = new[] { "a" } });
		Assert.That((await handler.ValidateAsync(connection.Id, parameters, CancellationToken.None)).Error.Code, Is.EqualTo("Connection.Forbidden"));
		access.Setup(x => x.CanAccessAsync(connection)).ReturnsAsync(true);
		connection.SetReadOnly(true);
		Assert.That((await handler.ValidateAsync(connection.Id, parameters, CancellationToken.None)).Error.Code, Is.EqualTo("Connection.ReadOnlyMode"));
	}
}

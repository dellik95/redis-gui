using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.RunLoadTest;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class RunLoadTestCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisLoadTestService> _loadTestServiceMock = null!;
	private RunLoadTestCommandHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_loadTestServiceMock = new Mock<IRedisLoadTestService>();
		_handler = new RunLoadTestCommandHandler(_repositoryMock.Object, _loadTestServiceMock.Object);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		Guid connectionId = Guid.NewGuid();
		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound));

		var command = new RunLoadTestCommand(connectionId, OperationsCount: 100);
		var result = await _handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.NotFound);
	}

	[Test]
	public async Task Handle_ReadOnlyConnection_WithWriteOperation_ReturnsReadOnlyError()
	{
		Guid connectionId = Guid.NewGuid();
		var conn = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			new ConnectionOwnerId(Guid.NewGuid())).Value;
		conn.SetReadOnly(true);

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(conn));

		var command = new RunLoadTestCommand(connectionId, OperationsCount: 100, OperationType: "write");
		var result = await _handler.Handle(command, CancellationToken.None);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Connection.ReadOnlyMode);
	}

	[Test]
	public async Task Handle_ValidRequest_ExecutesLoadTestAndReturnsResult()
	{
		Guid connectionId = Guid.NewGuid();
		var conn = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			new ConnectionOwnerId(Guid.NewGuid())).Value;

		var expectedResult = new RedisLoadTestResult
		{
			TotalOperations = 500,
			SuccessfulOperations = 500,
			FailedOperations = 0,
			TotalDurationMs = 45.2,
			OpsPerSecond = 11061.9,
			CleanedKeysCount = 250
		};

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(conn));

		_loadTestServiceMock
			.Setup(x => x.RunLoadTestAsync(conn, It.IsAny<RedisLoadTestRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(expectedResult));

		var command = new RunLoadTestCommand(connectionId, OperationsCount: 500, Concurrency: 5, OperationType: "mixed");
		var result = await _handler.Handle(command, CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.TotalOperations.Should().Be(500);
		result.Value.SuccessfulOperations.Should().Be(500);
		result.Value.CleanedKeysCount.Should().Be(250);
		_loadTestServiceMock.Verify(x => x.RunLoadTestAsync(conn, It.Is<RedisLoadTestRequest>(r => r.OperationsCount == 500), It.IsAny<CancellationToken>()), Times.Once);
	}
}

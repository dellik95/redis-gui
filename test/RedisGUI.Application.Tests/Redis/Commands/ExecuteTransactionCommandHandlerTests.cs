using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.ExecuteTransaction;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class ExecuteTransactionCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> connectionRepositoryMock = null!;
	private Mock<RedisGUI.Domain.Connection.IRedisTransactionService> connectionServiceMock = null!;
	private ExecuteTransactionCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connectionRepositoryMock = new Mock<IRedisConnectionRepository>(MockBehavior.Strict);
		connectionServiceMock = new Mock<RedisGUI.Domain.Connection.IRedisTransactionService>(MockBehavior.Strict);
		handler = new ExecuteTransactionCommandHandler(
			connectionRepositoryMock.Object,
			connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_AllOperationsSucceed_ReturnsSuccess()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var operations = new List<RedisOperation>
		{
			new RedisOperation("SET", "key1", "value1"),
			new RedisOperation("DEL", "key2")
		};

		var command = new ExecuteTransactionCommand(connectionId, operations);

		var connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		connectionServiceMock
			.Setup(x => x.ExecuteTransactionAsync(It.IsAny<RedisConnection>(), It.IsAny<List<RedisOperation>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(new List<string> { "OK", "1" }));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Success.Should().BeTrue();
		result.Value.Results.Should().HaveCount(2);
	}

	[Test]
	public async Task Handle_TransactionFails_ReturnsFailure()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var operations = new List<RedisOperation>
		{
			new RedisOperation("SET", "key1", "value1")
		};

		var command = new ExecuteTransactionCommand(connectionId, operations);

		var connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		var error = new Error("Redis.TransactionFailed", "Transaction execution failed");
		connectionServiceMock
			.Setup(x => x.ExecuteTransactionAsync(It.IsAny<RedisConnection>(), It.IsAny<List<RedisOperation>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<List<string>>(error));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(error);
	}

	[Test]
	public async Task Handle_MixedResults_ReturnsPartialSuccess()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var operations = new List<RedisOperation>
		{
			new RedisOperation("SET", "key1", "value1"),
			new RedisOperation("GET", "nonexistent")
		};

		var command = new ExecuteTransactionCommand(connectionId, operations);

		var connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		connectionServiceMock
			.Setup(x => x.ExecuteTransactionAsync(It.IsAny<RedisConnection>(), It.IsAny<List<RedisOperation>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(new List<string> { "OK", "" }));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Success.Should().BeTrue();
		result.Value.Results.Should().HaveCount(2);
	}
}





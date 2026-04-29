using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class DeleteMultipleKeysCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> connectionRepositoryMock = null!;
	private Mock<IRedisKeyService> connectionServiceMock = null!;
	private DeleteMultipleKeysCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
		connectionServiceMock = new Mock<IRedisKeyService>();
		handler = new DeleteMultipleKeysCommandHandler(
			connectionRepositoryMock.Object,
			connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_ValidCommand_ReturnsSuccessWithCounts()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var keys = new List<string> { "key1", "key2", "key3" }.AsReadOnly();
		var command = new DeleteMultipleKeysCommand(connectionId, keys);

		var connection = RedisConnection.Create(
			  new ConnectionName("Test Connection"),
			  new ConnectionHost("localhost"),
			  new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		var failedKeys = new List<string> { "key3" }.AsReadOnly();
		connectionServiceMock
			.Setup(x => x.DeleteMultipleKeysAsync(It.IsAny<RedisConnection>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success<(int, IReadOnlyList<string>)>((2, failedKeys)));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.DeletedCount.Should().Be(2);
		result.Value.TotalRequested.Should().Be(3);
		result.Value.FailedKeys.Should().HaveCount(1);
		result.Value.FailedKeys.Should().Contain("key3");
	}

	[Test]
	public async Task Handle_AllKeysDeleted_ReturnsNoFailures()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var keys = new List<string> { "key1", "key2" }.AsReadOnly();
		var command = new DeleteMultipleKeysCommand(connectionId, keys);

		var connection = RedisConnection.Create(
			  new ConnectionName("Test Connection"),
			  new ConnectionHost("localhost"),
			  new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		var failedKeys = new List<string>().AsReadOnly();
		connectionServiceMock
			.Setup(x => x.DeleteMultipleKeysAsync(It.IsAny<RedisConnection>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success<(int, IReadOnlyList<string>)>((2, failedKeys)));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.DeletedCount.Should().Be(2);
		result.Value.FailedKeys.Should().BeEmpty();
	}
}





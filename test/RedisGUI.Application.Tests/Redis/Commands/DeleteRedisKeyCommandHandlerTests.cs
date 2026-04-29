using FluentAssertions;
using Moq;
using RedisGUI.Application.Redis.Commands.DeleteRedisKey;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class DeleteRedisKeyCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisKeyService> _connectionServiceMock = null!;
	private DeleteRedisKeyCommandHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_connectionServiceMock = new Mock<IRedisKeyService>();
		_handler = new DeleteRedisKeyCommandHandler(
			_repositoryMock.Object,
			_connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_KeyExists_DeletesKey()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		DeleteRedisKeyCommand command = new(connectionId, "test:key");

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		_connectionServiceMock
			.Setup(x => x.DeleteKeyAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(true));

		// Act
		Result<bool> result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeTrue();
		_connectionServiceMock.Verify(x => x.DeleteKeyAsync(connection, "test:key"), Times.Once);
	}

	[Test]
	public async Task Handle_KeyNotFound_ReturnsFalse()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		DeleteRedisKeyCommand command = new(connectionId, "nonexistent:key");

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		_connectionServiceMock
			.Setup(x => x.DeleteKeyAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(false));

		// Act
		Result<bool> result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeFalse();
	}
}






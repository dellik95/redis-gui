using FluentAssertions;
using Moq;
using RedisGUI.Application.Redis.Commands.SetRedisValue;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class AddRedisValueCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisValueService> _connectionServiceMock = null!;
	private AddRedisValueCommandHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_connectionServiceMock = new Mock<IRedisValueService>();
		_handler = new AddRedisValueCommandHandler(
			_repositoryMock.Object,
			_connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_ValidRequest_SetsValue()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		AddRedisValueCommand command = new(
			connectionId,
			"test:key",
			"test-value",
			null);

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
			.Setup(x => x.SetValueAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		// Act
		Result result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		_connectionServiceMock.Verify(x => x.SetValueAsync(connection, "test:key", "test-value", null), Times.Once);
	}

	[Test]
	public async Task Handle_ConnectionNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		AddRedisValueCommand command = new(
			connectionId,
			"test:key",
			"test-value",
			null);

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Failure<RedisConnection>(DomainErrors.Connection.NotFound));

		// Act
		Result result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		_connectionServiceMock.Verify(x => x.SetValueAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Never);
	}
}






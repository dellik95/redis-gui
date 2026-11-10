using FluentAssertions;
using Moq;
using RedisGUI.Application.Redis.Commands.UpdateRedisValue;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class UpdateRedisValueCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisValueService> _connectionServiceMock = null!;
	private UpdateRedisValueCommandHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_connectionServiceMock = new Mock<IRedisValueService>();
		_handler = new UpdateRedisValueCommandHandler(
			_repositoryMock.Object,
			_connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_ValidUpdate_UpdatesValue()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		UpdateRedisValueCommand command = new(
			connectionId,
			"test:key",
			RedisDataType.String,
			new StringValueData("updated-value"),
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
			.Setup(x => x.UpdateValueAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		// Act
		Result result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		_connectionServiceMock.Verify(x => x.UpdateValueAsync(connection, "test:key", "updated-value", "String", null), Times.Once);
	}
}






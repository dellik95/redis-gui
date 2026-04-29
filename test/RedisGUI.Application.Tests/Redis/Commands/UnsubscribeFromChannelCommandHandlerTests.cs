using FluentAssertions;
using Moq;
using RedisGUI.Application.Redis.Commands.UnsubscribeFromChannel;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class UnsubscribeFromChannelCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisPubSubService> _connectionServiceMock = null!;
	private UnsubscribeFromChannelCommandHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_connectionServiceMock = new Mock<IRedisPubSubService>();
		_handler = new UnsubscribeFromChannelCommandHandler(
			_repositoryMock.Object,
			_connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_ValidUnsubscribe_Unsubscribes()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		string subscriptionId = "subscription-id";
		UnsubscribeFromChannelCommand command = new(connectionId, subscriptionId);

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
			.Setup(x => x.UnsubscribeFromChannelAsync(connection, subscriptionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success());

		// Act
		Result result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		_connectionServiceMock.Verify(x => x.UnsubscribeFromChannelAsync(connection, subscriptionId, It.IsAny<CancellationToken>()), Times.Once);
	}
}







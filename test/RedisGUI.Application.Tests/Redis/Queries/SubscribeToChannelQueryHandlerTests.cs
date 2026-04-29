using FluentAssertions;
using Moq;
using RedisGUI.Application.Redis.Queries.SubscribeToChannel;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Queries;

[TestFixture]
public class SubscribeToChannelQueryHandlerTests
{
	private Mock<IRedisConnectionRepository> _repositoryMock = null!;
	private Mock<IRedisPubSubService> _connectionServiceMock = null!;
	private SubscribeToChannelQueryHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IRedisConnectionRepository>();
		_connectionServiceMock = new Mock<IRedisPubSubService>();
		_handler = new SubscribeToChannelQueryHandler(
			_repositoryMock.Object,
			_connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_ValidSubscription_ReturnsSubscriptionId()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		string channel = "test:channel";
		SubscribeToChannelQuery query = new(connectionId, channel);

		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value!;

		string subscriptionId = Guid.NewGuid().ToString();

		_repositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		_connectionServiceMock
			.Setup(x => x.SubscribeToChannelAsync(connection, channel, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(subscriptionId));

		// Act
		Result<SubscribeToChannelResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.SubscriptionId.Should().Be(subscriptionId);
		result.Value.Channel.Should().Be(channel);
		result.Value.Success.Should().BeTrue();
	}
}







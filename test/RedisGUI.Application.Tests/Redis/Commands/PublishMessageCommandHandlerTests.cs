using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.PublishMessage;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class PublishMessageCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> connectionRepositoryMock = null!;
	private Mock<IRedisPubSubService> connectionServiceMock = null!;
	private PublishMessageCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
		connectionServiceMock = new Mock<IRedisPubSubService>();
		handler = new PublishMessageCommandHandler(
			connectionRepositoryMock.Object,
			connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_ValidCommand_PublishesSuccessfully()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var channel = "notifications";
		var message = "Test message";
		var command = new PublishMessageCommand(connectionId, channel, message);

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
			.Setup(x => x.PublishMessageAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			  .ReturnsAsync(Result.Success());

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
	}

	[Test]
	public async Task Handle_NoSubscribers_ReturnsZero()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var command = new PublishMessageCommand(connectionId, "empty-channel", "message");

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
			.Setup(x => x.PublishMessageAsync(connection, It.IsAny<string>(), It.IsAny<string>()))
			  .ReturnsAsync(Result.Success());

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
	}
}






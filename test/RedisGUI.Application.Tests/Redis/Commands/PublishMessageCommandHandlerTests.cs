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
    private Mock<IRedisConnectionRepository> _connectionRepositoryMock = null!;
    private Mock<IConnectionService> _connectionServiceMock = null!;
    private PublishMessageCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
        _connectionServiceMock = new Mock<IConnectionService>();
        _handler = new PublishMessageCommandHandler(
            _connectionRepositoryMock.Object,
            _connectionServiceMock.Object);
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
            0,
            null).Value;

        _connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(connection));

        _connectionServiceMock
            .Setup(x => x.PublishMessage(connection, channel, message))
              .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

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
            0,
            null).Value;

        _connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(connection));

        _connectionServiceMock
            .Setup(x => x.PublishMessage(connection, It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}

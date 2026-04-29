using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class DeleteKeysByPatternCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> connectionRepositoryMock = null!;
	private Mock<IRedisKeyService> connectionServiceMock = null!;
	private DeleteKeysByPatternCommandHandler handler = null!;

	[SetUp]
    public void Setup()
    {
        connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
        connectionServiceMock = new Mock<IRedisKeyService>();
        handler = new DeleteKeysByPatternCommandHandler(
            connectionRepositoryMock.Object,
            connectionServiceMock.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_ReturnsSuccessWithDeletedCount()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var pattern = "user:*";
        var command = new DeleteKeysByPatternCommand(connectionId, pattern);
        
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
            .Setup(x => x.DeleteKeysByPatternAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(5));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DeletedCount.Should().Be(5);
        result.Value.Pattern.Should().Be(pattern);
    }

    [Test]
    public async Task Handle_ConnectionNotFound_ReturnsFailure()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var command = new DeleteKeysByPatternCommand(connectionId, "test:*");
        
        var error = new Error("Connection.NotFound", "Connection not found");
        connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<RedisConnection>(error));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Test]
    public async Task Handle_DeleteFails_ReturnsFailure()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var command = new DeleteKeysByPatternCommand(connectionId, "test:*");
        
        var connection = RedisConnection.Create(
            new ConnectionName("Test Connection"),
            new ConnectionHost("localhost"),
            new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

        connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(connection));

        var error = new Error("Redis.DeleteFailed", "Delete operation failed");
        connectionServiceMock
            .Setup(x => x.DeleteKeysByPatternAsync(It.IsAny<RedisConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<int>(error));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}





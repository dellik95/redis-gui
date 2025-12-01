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
    private Mock<IRedisConnectionRepository> _connectionRepositoryMock = null!;
    private Mock<IConnectionService> _connectionServiceMock = null!;
    private DeleteKeysByPatternCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
        _connectionServiceMock = new Mock<IConnectionService>();
        _handler = new DeleteKeysByPatternCommandHandler(
            _connectionRepositoryMock.Object,
            _connectionServiceMock.Object);
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
            0,
            null).Value;

        _connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(connection));

        _connectionServiceMock
            .Setup(x => x.DeleteKeysByPattern(connection, pattern))
            .ReturnsAsync(Result.Success(5));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

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
        _connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<RedisConnection>(error));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

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
            0,
            null).Value;

        _connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(connection));

        var error = new Error("Redis.DeleteFailed", "Delete operation failed");
        _connectionServiceMock
            .Setup(x => x.DeleteKeysByPattern(connection, It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<int>(error));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}

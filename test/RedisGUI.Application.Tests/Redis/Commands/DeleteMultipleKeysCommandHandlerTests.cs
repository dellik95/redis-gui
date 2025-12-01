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
    private Mock<IRedisConnectionRepository> _connectionRepositoryMock = null!;
    private Mock<IConnectionService> _connectionServiceMock = null!;
    private DeleteMultipleKeysCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
        _connectionServiceMock = new Mock<IConnectionService>();
        _handler = new DeleteMultipleKeysCommandHandler(
            _connectionRepositoryMock.Object,
            _connectionServiceMock.Object);
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
            0,
            null).Value;

        _connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(connection));

        var failedKeys = new List<string> { "key3" }.AsReadOnly();
        _connectionServiceMock
            .Setup(x => x.DeleteMultipleKeys(connection, keys))
            .ReturnsAsync(Result.Success<(int, IReadOnlyList<string>)>((2, failedKeys)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

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
            0,
            null).Value;

        _connectionRepositoryMock
            .Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(connection));

        var failedKeys = new List<string>().AsReadOnly();
        _connectionServiceMock
            .Setup(x => x.DeleteMultipleKeys(connection, keys))
            .ReturnsAsync(Result.Success<(int, IReadOnlyList<string>)>((2, failedKeys)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DeletedCount.Should().Be(2);
        result.Value.FailedKeys.Should().BeEmpty();
    }
}

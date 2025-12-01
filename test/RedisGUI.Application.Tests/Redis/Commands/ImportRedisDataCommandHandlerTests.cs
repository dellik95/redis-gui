using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.ImportRedisData;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class ImportRedisDataCommandHandlerTests
{
    private Mock<IRedisConnectionRepository> _connectionRepositoryMock = null!;
    private Mock<IConnectionService> _connectionServiceMock = null!;
    private ImportRedisDataCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
        _connectionServiceMock = new Mock<IConnectionService>();
        _handler = new ImportRedisDataCommandHandler(
            _connectionRepositoryMock.Object,
            _connectionServiceMock.Object);
    }

    [Test]
    public async Task Handle_JsonFormat_ImportsSuccessfully()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var items = new[]
        {
            new RedisImportItem { Key = "key1", Value = "value1", Type = "String" },
            new RedisImportItem { Key = "key2", Value = "value2", Type = "String", TimeToLive = 60 }
        };
        var jsonData = JsonSerializer.Serialize(items);
        var command = new ImportRedisDataCommand(connectionId, jsonData, ImportFormat.Json, true);
        
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
            .Setup(x => x.SetValue(connection, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(2);
        result.Value.SkippedCount.Should().Be(0);
        result.Value.FailedCount.Should().Be(0);
    }

    [Test]
    public async Task Handle_CsvFormat_ImportsSuccessfully()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var csvData = "Key,Value,Type,TTL\n\"key1\",\"value1\",\"String\",\"\"\n\"key2\",\"value2\",\"String\",\"60\"";
        var command = new ImportRedisDataCommand(connectionId, csvData, ImportFormat.Csv, true);
        
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
            .Setup(x => x.SetValue(connection, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(2);
    }

    [Test]
    public async Task Handle_OverwriteFalse_SkipsExistingKeys()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var items = new[]
        {
            new RedisImportItem { Key = "existing", Value = "value1", Type = "String" }
        };
        var jsonData = JsonSerializer.Serialize(items);
        var command = new ImportRedisDataCommand(connectionId, jsonData, ImportFormat.Json, false);
        
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
            .Setup(x => x.GetValue(connection, "existing"))
            .ReturnsAsync(Result.Success(RedisValue.Create("existing", "old", "String").Value));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImportedCount.Should().Be(0);
        result.Value.SkippedCount.Should().Be(1);
    }
}

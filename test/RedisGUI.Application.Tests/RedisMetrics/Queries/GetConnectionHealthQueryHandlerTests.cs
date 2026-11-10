using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Connections.GetConnectionHealth;
using RedisGUI.Domain.Connection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Connections.Queries;

[TestFixture]
public class GetConnectionHealthQueryHandlerTests
{
    private Mock<IConnectionHealthMonitor> _healthMonitorMock = null!;
    private GetConnectionHealthQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _healthMonitorMock = new Mock<IConnectionHealthMonitor>();
        _handler = new GetConnectionHealthQueryHandler(_healthMonitorMock.Object);
    }

    [Test]
    public async Task Handle_HealthyConnection_ReturnsHealthStatus()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var query = new GetConnectionHealthQuery(connectionId);
        
            var healthStatus = ConnectionHealth.Healthy(
            connectionId,
                25.5);

        _healthMonitorMock
            .Setup(x => x.GetHealth(connectionId))
            .Returns(healthStatus);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ConnectionId.Should().Be(connectionId);
        result.Value.Status.Should().Be("Healthy");
        result.Value.ResponseTimeMs.Should().Be(25.5);
        result.Value.ConsecutiveFailures.Should().Be(0);
    }

    [Test]
    public async Task Handle_UnhealthyConnection_ReturnsErrorStatus()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var query = new GetConnectionHealthQuery(connectionId);
        
            var healthStatus = ConnectionHealth.Unhealthy(
            connectionId,
            "Connection timeout",
            3);

        _healthMonitorMock
            .Setup(x => x.GetHealth(connectionId))
            .Returns(healthStatus);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Unhealthy");
        result.Value.ErrorMessage.Should().Be("Connection timeout");
        result.Value.ConsecutiveFailures.Should().Be(3);
    }

    [Test]
    public async Task Handle_NoHealthInfo_ReturnsFailure()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var query = new GetConnectionHealthQuery(connectionId);
        
        _healthMonitorMock
            .Setup(x => x.GetHealth(connectionId))
                .Returns((ConnectionHealth?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Health.NotFound");
    }
}

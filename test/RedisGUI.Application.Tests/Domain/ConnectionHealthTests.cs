using FluentAssertions;
using NUnit.Framework;
using RedisGUI.Domain.Connection;
using System;

namespace RedisGUI.Application.Tests.Domain;

[TestFixture]
public class ConnectionHealthTests
{
    [Test]
    public void Healthy_HealthyConnection_SetsPropertiesCorrectly()
    {
        // Arrange
        var connectionId = Guid.NewGuid();

        // Act
        var health = ConnectionHealth.Healthy(connectionId, 15.5);

        // Assert
        health.ConnectionId.Should().Be(connectionId);
        health.Status.Should().Be(ConnectionHealthStatus.Healthy);
        health.ResponseTimeMs.Should().Be(15.5);
        health.ErrorMessage.Should().BeNull();
        health.ConsecutiveFailures.Should().Be(0);
        health.LastChecked.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Unhealthy_UnhealthyConnection_SetsErrorMessage()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var errorMessage = "Connection timeout";

        // Act
        var health = ConnectionHealth.Unhealthy(connectionId, errorMessage, 3);

        // Assert
        health.ConnectionId.Should().Be(connectionId);
        health.Status.Should().Be(ConnectionHealthStatus.Unhealthy);
        health.ResponseTimeMs.Should().Be(0);
        health.ErrorMessage.Should().Be(errorMessage);
        health.ConsecutiveFailures.Should().Be(3);
    }

    [Test]
    public void Degraded_DegradedConnection_SetsReasonAndStatus()
    {
        // Arrange
        var connectionId = Guid.NewGuid();
        var reason = "High latency detected";

        // Act
        var health = ConnectionHealth.Degraded(connectionId, 150.0, reason);

        // Assert
        health.ConnectionId.Should().Be(connectionId);
        health.Status.Should().Be(ConnectionHealthStatus.Degraded);
        health.ResponseTimeMs.Should().Be(150.0);
        health.ErrorMessage.Should().Be(reason);
        health.ConsecutiveFailures.Should().Be(0);
    }
}

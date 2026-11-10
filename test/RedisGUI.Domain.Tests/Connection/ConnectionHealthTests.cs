using RedisGUI.Domain.Connection;
using System;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Tests for ConnectionHealth value object
/// </summary>
public class ConnectionHealthTests
{
	[Test]
	public void Healthy_ShouldCreateHealthyStatus()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		double responseTimeMs = 10.5;

		// Act
		ConnectionHealth health = ConnectionHealth.Healthy(connectionId, responseTimeMs);

		// Assert
		health.ConnectionId.Should().Be(connectionId);
		health.Status.Should().Be(ConnectionHealthStatus.Healthy);
		health.ResponseTimeMs.Should().Be(responseTimeMs);
		health.ErrorMessage.Should().BeNull();
		health.ConsecutiveFailures.Should().Be(0);
		health.LastChecked.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Test]
	public void Unhealthy_ShouldCreateUnhealthyStatus()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		string errorMessage = "Connection failed";
		int consecutiveFailures = 3;

		// Act
		ConnectionHealth health = ConnectionHealth.Unhealthy(connectionId, errorMessage, consecutiveFailures);

		// Assert
		health.ConnectionId.Should().Be(connectionId);
		health.Status.Should().Be(ConnectionHealthStatus.Unhealthy);
		health.ErrorMessage.Should().Be(errorMessage);
		health.ConsecutiveFailures.Should().Be(consecutiveFailures);
		health.ResponseTimeMs.Should().Be(0);
		health.LastChecked.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Test]
	public void Degraded_ShouldCreateDegradedStatus()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		double responseTimeMs = 100.0;
		string reason = "High latency";

		// Act
		ConnectionHealth health = ConnectionHealth.Degraded(connectionId, responseTimeMs, reason);

		// Assert
		health.ConnectionId.Should().Be(connectionId);
		health.Status.Should().Be(ConnectionHealthStatus.Degraded);
		health.ResponseTimeMs.Should().Be(responseTimeMs);
		health.ErrorMessage.Should().Be(reason);
		health.ConsecutiveFailures.Should().Be(0);
		health.LastChecked.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}
}


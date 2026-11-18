namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents the health status of a Redis connection
/// </summary>
public enum ConnectionHealthStatus
{
	/// <summary>
	/// Connection health is unknown or not yet checked
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Connection is healthy and responding
	/// </summary>
	Healthy = 1,

	/// <summary>
	/// Connection is degraded but still functional
	/// </summary>
	Degraded = 2,

	/// <summary>
	/// Connection is unhealthy
	/// </summary>
	Unhealthy = 3
}

/// <summary>
/// Represents the health information for a Redis connection
/// </summary>
public sealed class ConnectionHealth
{
	/// <summary>
	/// Connection ID
	/// </summary>
	public Guid ConnectionId { get; init; }

	/// <summary>
	/// Current health status
	/// </summary>
	public ConnectionHealthStatus Status { get; init; }

	/// <summary>
	/// Last check timestamp
	/// </summary>
	public DateTime LastChecked { get; init; }

	/// <summary>
	/// Response time in milliseconds
	/// </summary>
	public double ResponseTimeMs { get; init; }

	/// <summary>
	/// Error message if unhealthy
	/// </summary>
	public string? ErrorMessage { get; init; }

	/// <summary>
	/// Number of consecutive failures
	/// </summary>
	public int ConsecutiveFailures { get; init; }

	/// <summary>
	/// Confirmed server deployment topology (Standalone, Cluster, Sentinel, or Unknown).
	/// </summary>
	public string ServerMode { get; init; } = nameof(RedisServerMode.Unknown);

	private ConnectionHealth() { }

	/// <summary>
	/// Create a healthy status
	/// </summary>
	public static ConnectionHealth Healthy(Guid connectionId, double responseTimeMs, string serverMode = nameof(RedisServerMode.Unknown))
	{
		return new ConnectionHealth
		{
			ConnectionId = connectionId,
			Status = ConnectionHealthStatus.Healthy,
			LastChecked = DateTime.UtcNow,
			ResponseTimeMs = responseTimeMs,
			ErrorMessage = null,
			ConsecutiveFailures = 0,
			ServerMode = serverMode
		};
	}

	/// <summary>
	/// Create an unhealthy status
	/// </summary>
	public static ConnectionHealth Unhealthy(Guid connectionId, string errorMessage, int consecutiveFailures, string serverMode = nameof(RedisServerMode.Unknown))
	{
		return new ConnectionHealth
		{
			ConnectionId = connectionId,
			Status = ConnectionHealthStatus.Unhealthy,
			LastChecked = DateTime.UtcNow,
			ResponseTimeMs = 0,
			ErrorMessage = errorMessage,
			ConsecutiveFailures = consecutiveFailures,
			ServerMode = serverMode
		};
	}

	/// <summary>
	/// Create a degraded status
	/// </summary>
	public static ConnectionHealth Degraded(Guid connectionId, double responseTimeMs, string reason, string serverMode = nameof(RedisServerMode.Unknown))
	{
		return new ConnectionHealth
		{
			ConnectionId = connectionId,
			Status = ConnectionHealthStatus.Degraded,
			LastChecked = DateTime.UtcNow,
			ResponseTimeMs = responseTimeMs,
			ErrorMessage = reason,
			ConsecutiveFailures = 0,
			ServerMode = serverMode
		};
	}
}

namespace RedisGUI.Infrastructure.Redis.ConnectionManager;

/// <summary>
/// Snapshot of runtime state and telemetry for a managed Redis connection.
/// </summary>
public sealed record RedisConnectionInfo(
	string Key,
	string SanitizedEndpoint,
	RedisConnectionState State,
	DateTimeOffset? LastConnectedAt,
	DateTimeOffset? LastDisconnectedAt,
	DateTimeOffset LastUsedAt,
	TimeSpan? InitializationDuration,
	string? LastFailure,
	bool IsConnected,
	int ActiveOperations
);

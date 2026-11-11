namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Aggregate statistics for the Redis connection manager.
/// </summary>
public sealed record RedisConnectionManagerStatistics(
	int TotalCachedConnections,
	int ActiveConnections,
	long ConnectionCreationCount,
	long ConnectionFailureCount,
	long ConnectionRestorationCount,
	long EvictionCount
);

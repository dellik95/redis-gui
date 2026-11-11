namespace RedisGUI.Infrastructure.Redis.ConnectionManager;

/// <summary>
/// Configuration options for the Redis connection manager.
/// </summary>
public class RedisConnectionManagerOptions
{
	/// <summary>
	/// Configuration key for connection manager settings.
	/// </summary>
	public const string SectionKey = "Redis:ConnectionManager";

	/// <summary>
	/// Maximum number of concurrent active multiplexers allowed in the registry.
	/// </summary>
	public int MaxConnections { get; set; } = 50;

	/// <summary>
	/// Timeout duration for establishing new connections.
	/// </summary>
	public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Duration after which idle connections become eligible for eviction.
	/// </summary>
	public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Interval between periodic background checks for idle / stale connections.
	/// </summary>
	public TimeSpan EvictionInterval { get; set; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Grace period after eviction or configuration replacement before closing and disposing the multiplexer,
	/// allowing in-flight Redis operations to complete cleanly.
	/// </summary>
	public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromSeconds(2);

	/// <summary>
	/// Whether to allow fallback from Docker host 'redis' to '127.0.0.1' in local development environments.
	/// Defaults to false so arbitrary endpoints do not mask misconfigurations.
	/// </summary>
	public bool EnableDockerFallback { get; set; } = false;
}

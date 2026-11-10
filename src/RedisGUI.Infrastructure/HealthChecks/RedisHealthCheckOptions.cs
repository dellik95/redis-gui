namespace RedisGUI.Infrastructure.HealthChecks;

/// <summary>
/// Configuration for Redis health check.
/// </summary>
public class RedisHealthCheckOptions
{
	/// <summary>
	/// Configuration key.
	/// </summary>
	public const string Key = "Redis:HealthCheck";

	/// <summary>
	/// Redis connection string used for health probing (e.g. "localhost:6379").
	/// </summary>
	public string? ConnectionString { get; set; }

	/// <summary>
	/// Database index used for the ping.
	/// </summary>
	public int Database { get; set; } = 0;
}


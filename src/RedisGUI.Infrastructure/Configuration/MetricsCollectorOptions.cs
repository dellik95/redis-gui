namespace RedisGUI.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the Redis metrics collector
/// </summary>
public class MetricsCollectorOptions
{
	/// <summary>
	/// Configuration key used to identify metrics collector settings in configuration sources
	/// </summary>
	public const string Key = "MetricsCollector";

	/// <summary>
	/// Gets or sets the interval in seconds between metrics collection cycles
	/// </summary>
	public int PoolingIntervalInSec { get; set; } = 1;
}

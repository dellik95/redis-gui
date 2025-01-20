namespace RedisGUI.Infrastructure.Configuration;

public class MetricsCollectorOptions
{
	public const string Key = "MetricsCollector";

	public int PoolingIntervalInSec { get; set; } = 1;
}

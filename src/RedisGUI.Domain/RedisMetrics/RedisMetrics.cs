using RedisGUI.Domain.RedisMetrics.Attributes;

namespace RedisGUI.Domain.RedisMetrics;

public class RedisMetrics
{
	[RedisMetric("Stats", "instantaneous_input_kbps")]
	public double NetworkSpeedIn { get; set; }

	[RedisMetric("Stats", "instantaneous_output_kbps")]
	public double NetworkSpeedOut { get; set; }

	[RedisMetric("Keyspace", valuePattern: @"keys=(\d+)")]
	public long KeyCount { get; set; }

	[RedisMetric("Memory", "used_memory")]
	public double MemoryUsage { get; set; }

	[RedisMetric("CPU", "used_cpu_sys")]
	public double CpuUsageSystem { get; set; }

	[RedisMetric("CPU", "used_cpu_user")]
	public double CpuUsageUser { get; set; }

	[RedisMetric("CPU", "used_cpu_in_percent", true, providerType: ValueProviderType.CpuUsage)]
	public double CpuUsageInPercent { get; set; }

	[RedisMetric("Server", "uptime_in_seconds")]
	public double UptimeInSeconds { get; set; }
}

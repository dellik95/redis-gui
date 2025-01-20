using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.RedisMetrics.Queries
{
	public class GetRedisMetricsResponse
	{
		public double NetworkSpeedIn { get; set; }

		public double NetworkSpeedOut { get; set; }

		public long KeyCount { get; set; }

		public double MemoryUsage { get; set; }

		public double CpuUsageSystem { get; set; }

		public double CpuUsageUser { get; set; }

		public double CpuUsageInPercent { get; set; }

		public static Result<GetRedisMetricsResponse> FromDomainMetrics(Domain.RedisMetrics.RedisMetrics metrics) => new GetRedisMetricsResponse()
		{
			CpuUsageInPercent = metrics.CpuUsageInPercent,
			NetworkSpeedOut = metrics.NetworkSpeedOut,
			NetworkSpeedIn = metrics.NetworkSpeedIn,
			CpuUsageSystem = metrics.CpuUsageSystem,
			CpuUsageUser = metrics.CpuUsageUser,
			KeyCount = metrics.KeyCount,
			MemoryUsage = metrics.MemoryUsage
		};
	}
}

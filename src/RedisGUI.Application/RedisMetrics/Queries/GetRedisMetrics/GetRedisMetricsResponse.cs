namespace RedisGUI.Application.RedisMetrics.Queries.GetRedisMetrics
{
	public record GetRedisMetricsResponse(double Cpu, double MemoryUsage, int ConnectedClients, double NetworkSpeed, long KeyCount);
}

using RedisGUI.Domain.Connection;

namespace RedisGUI.Domain.RedisMetrics.Abstractions;

public interface IMetricsCollector
{
	Task<Result<RedisMetrics>> CollectMetrics(RedisConnection connection, CancellationToken cancellationToken = default);
}

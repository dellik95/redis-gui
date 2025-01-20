using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.RedisMetrics.Abstractions;

public interface IMetricsCollector
{
	Task<Result<RedisMetrics>> CollectMetrics(RedisConnection connection, CancellationToken cancellationToken = default);
}

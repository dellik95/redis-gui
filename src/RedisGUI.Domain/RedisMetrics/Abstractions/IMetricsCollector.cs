using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.RedisMetrics.Abstractions;

public interface IMetricsCollector
{
	Task<Result<RedisMetrics>> CollectMetrics(RedisConnection connection, CancellationToken cancellationToken = default);
}

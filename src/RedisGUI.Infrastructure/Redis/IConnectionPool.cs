using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

public interface IConnectionPool : IDisposable, IAsyncDisposable
{
	Task<ConnectionMultiplexer> GetConnection(ConfigurationOptions redisConnection);
}
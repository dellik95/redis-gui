using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Connection;

public interface IRedisConnectionRepository : IRepository<RedisConnection>
{
	Task<Result<RedisConnection>> GetConnectionByIdAsync(Guid id, CancellationToken token = default);
}
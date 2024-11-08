using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represent repository contract for Redis connection entity
/// </summary>
public interface IRedisConnectionRepository : IRepository<RedisConnection>
{
	/// <summary>
	/// Get connection entity by id
	/// </summary>
	/// <param name="id">Connection id</param>
	/// <param name="token">Cancellation token</param>
	/// <returns></returns>
	Task<Result<RedisConnection>> GetConnectionByIdAsync(Guid id, CancellationToken token = default);
}

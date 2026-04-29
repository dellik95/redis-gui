using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for managing Redis transaction operations
/// </summary>
public interface IRedisTransactionService
{
	/// <summary>
	/// Execute a Redis transaction (MULTI/EXEC)
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="operations">List of operations to execute atomically</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Results from each operation</returns>
	Task<Result<List<string>>> ExecuteTransactionAsync(RedisConnection connection, List<RedisOperation> operations, CancellationToken cancellationToken = default);
}

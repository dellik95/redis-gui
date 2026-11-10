using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for managing Redis connection health and testing
/// </summary>
public interface IRedisConnectionService
{
	/// <summary>
	/// Verifies if a Redis connection can be established
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> CheckConnectionAsync(RedisConnection connection, CancellationToken cancellationToken = default);
}

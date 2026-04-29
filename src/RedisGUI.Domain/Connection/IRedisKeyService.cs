using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for managing Redis key operations
/// </summary>
public interface IRedisKeyService
{
	/// <summary>
	/// Returns all keys from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of key names</returns>
	ValueTask<Result<IEnumerable<string>>> GetKeysAsync(RedisConnection connection, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete a single key from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>True if deleted, false if key didn't exist</returns>
	Task<Result<bool>> DeleteKeyAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete multiple keys from redis by their exact names
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="keys">Keys to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Number of keys deleted</returns>
	Task<Result<long>> DeleteKeysAsync(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete multiple keys from redis by their exact names with detailed results
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="keys">Keys to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Tuple with deleted count and list of failed keys</returns>
	Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeysAsync(
		RedisConnection connection, 
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete keys from redis matching a pattern
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="pattern">Pattern to match keys (e.g., "user:*")</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Number of keys deleted</returns>
	Task<Result<int>> DeleteKeysByPatternAsync(RedisConnection connection, string pattern, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get the type of key in redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key to get the type of</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Type name (String, List, Set, Hash, SortedSet, Stream)</returns>
	Task<Result<string>> GetKeyTypeAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default);
}

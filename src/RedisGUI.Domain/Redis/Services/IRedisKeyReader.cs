using RedisGUI.Domain.Connection;

namespace RedisGUI.Domain.Redis.Services;

/// <summary>
/// Domain service for reading Redis keys and values
/// </summary>
public interface IRedisKeyReader
{
	/// <summary>
	/// Retrieves all keys from the Redis database
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <returns>Collection of key names</returns>
	ValueTask<IEnumerable<string>> GetKeysAsync(RedisConnection connection);

	/// <summary>
	/// Retrieves value for a specific key
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="key">The key to retrieve</param>
	/// <returns>Result containing the Redis value</returns>
	Task<Result<RedisValue>> GetValueAsync(RedisConnection connection, string key);

	/// <summary>
	/// Retrieves multiple values for given keys
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="keys">The keys to retrieve</param>
	/// <returns>Dictionary of key-value pairs</returns>
	Task<Result<Dictionary<string, RedisValue>>> GetMultipleValuesAsync(RedisConnection connection, string[] keys);

	/// <summary>
	/// Gets keys matching a pattern
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="pattern">The pattern to match</param>
	/// <returns>Collection of matching keys</returns>
	ValueTask<IEnumerable<string>> GetKeysByPatternAsync(RedisConnection connection, string pattern);
}

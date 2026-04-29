using RedisGUI.Domain.Connection;

namespace RedisGUI.Domain.Redis.Services;

/// <summary>
/// Domain service for writing and deleting Redis keys
/// </summary>
public interface IRedisKeyWriter
{
	/// <summary>
	/// Sets a value for a specific key
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="key">The key to set</param>
	/// <param name="value">The value to set</param>
	/// <param name="ttl">Optional time-to-live</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> SetValueAsync(RedisConnection connection, string key, string value, TimeSpan? ttl = null);

	/// <summary>
	/// Updates an existing value
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="key">The key to update</param>
	/// <param name="value">The new value</param>
	/// <param name="ttl">Optional time-to-live</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> UpdateValueAsync(RedisConnection connection, string key, string value, TimeSpan? ttl = null);

	/// <summary>
	/// Deletes multiple keys
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="keys">The keys to delete</param>
	/// <returns>Number of keys deleted</returns>
	Task<long> DeleteKeysAsync(RedisConnection connection, string[] keys);

	/// <summary>
	/// Deletes keys matching a pattern
	/// </summary>
	/// <param name="connection">The Redis connection</param>
	/// <param name="pattern">The pattern to match</param>
	/// <returns>Number of keys deleted</returns>
	Task<long> DeleteKeysByPatternAsync(RedisConnection connection, string pattern);
}

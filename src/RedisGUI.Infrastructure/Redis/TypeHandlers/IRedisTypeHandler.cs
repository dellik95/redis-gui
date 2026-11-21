using FluentResults;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Strategy interface for reading, writing, and serializing a specific Redis data type.
/// </summary>
public interface IRedisTypeHandler
{
	/// <summary>
	/// Gets the RedisType supported by this handler.
	/// </summary>
	RedisType Type { get; }

	/// <summary>
	/// Reads the raw value from the Redis database.
	/// </summary>
	/// <param name="database">Redis database instance.</param>
	/// <param name="key">Target key.</param>
	/// <returns>Raw result from Redis command.</returns>
	Task<Result<object?>> ReadRawAsync(IDatabase database, string key);

	/// <summary>Reads the raw value using a binary-safe <see cref="RedisKey"/>.</summary>
	Task<Result<object?>> ReadRawAsync(IDatabase database, RedisKey key) => ReadRawAsync(database, key.ToString());

	/// <summary>
	/// Serializes the raw value into a JSON or string representation suitable for the UI editor.
	/// </summary>
	/// <param name="rawValue">Raw value retrieved from ReadRawAsync.</param>
	/// <returns>Serialized string.</returns>
	string Serialize(object? rawValue);

	/// <summary>
	/// Writes or updates the value in the Redis database.
	/// </summary>
	/// <param name="database">Redis database instance.</param>
	/// <param name="key">Target key.</param>
	/// <param name="value">New value from the UI.</param>
	/// <param name="ttl">Optional TTL.</param>
	/// <returns>Result indicating success or failure.</returns>
	Task<Result> WriteAsync(IDatabase database, string key, object value, TimeSpan? ttl);

	/// <summary>Writes or updates the value using a binary-safe <see cref="RedisKey"/>.</summary>
	Task<Result> WriteAsync(IDatabase database, RedisKey key, object value, TimeSpan? ttl) =>
		WriteAsync(database, key.ToString(), value, ttl);
}

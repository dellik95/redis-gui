using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represent connection service
/// </summary>
public interface IConnectionService
{
	/// <summary>
	/// Check connection is available
	/// </summary>
	/// <param name="connection">Connection</param>
	/// <returns></returns>
	Task<Result> CheckConnection(RedisConnection connection);

	/// <summary>
	/// Returns all keys from redis
	/// </summary>
	/// <param name="connection"></param>
	/// <returns></returns>
	ValueTask<IEnumerable<string>> GetKeys(RedisConnection connection);

	/// <summary>
	/// Delete keys from redis
	/// </summary>
	/// <param name="connection">Redis connection></param>
	/// <param name="keys">Keys to delete</param>
	/// <returns></returns>
	Task<long> DeleteKeys(RedisConnection connection, string[] keys);

	/// <summary>
	/// Set a value in redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key</param>
	/// <param name="value">Value</param>
	/// <param name="ttl">Time to live</param>
	/// <returns></returns>
	Task<Result> SetValue(RedisConnection connection, string key, string value, TimeSpan? ttl = null);

	/// <summary>
	/// Get a value from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key</param>
	/// <returns></returns>
	Task<Result<Redis.RedisValue>> GetValue(RedisConnection connection, string key);

	/// <summary>
	/// Get all keys and values from redis with pattern and pagination
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="pattern">Pattern to filter keys</param>
	/// <param name="pageSize">Number of items per page</param>
	/// <param name="pageNumber">Page number</param>
	/// <returns></returns>
	Task<Result<PagedRedisValues>> GetAllValues(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null);

	/// <summary>
	/// Delete a key from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key to delete</param>
	/// <returns></returns>
	Task<Result<bool>> DeleteKey(RedisConnection connection, string key);

	/// <summary>
	/// Delete multiple keys from redis by their exact names
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="keys">Keys to delete</param>
	/// <returns>Tuple with deleted count and list of failed keys</returns>
	Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeys(RedisConnection connection, IReadOnlyList<string> keys);

	/// <summary>
	/// Delete keys from redis matching a pattern
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="pattern">Pattern to match keys (e.g., "user:*")</param>
	/// <returns>Number of keys deleted</returns>
	Task<Result<int>> DeleteKeysByPattern(RedisConnection connection, string pattern);

	/// <summary>
	/// Get the type of key in redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key to get the type of</param>
	/// <returns></returns>
	Task<Result<string>> GetKeyType(RedisConnection connection, string key);

	/// <summary>
	/// Update a value in redis with type-specific handling
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key</param>
	/// <param name="value">Value (format depends on type)</param>
	/// <param name="type">Redis data type</param>
	/// <param name="ttl">Time to live</param>
	/// <returns></returns>
	Task<Result> UpdateValue(RedisConnection connection, string key, object value, string type, TimeSpan? ttl = null);

	/// <summary>
	/// Publish a message to a Redis Pub/Sub channel
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="channel">Channel name</param>
	/// <param name="message">Message to publish</param>
	/// <returns></returns>
	Task<Result> PublishMessage(RedisConnection connection, string channel, string message);

	/// <summary>
	/// Subscribe to a Redis Pub/Sub channel
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="channel">Channel name</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Subscription ID</returns>
	Task<Result<string>> SubscribeToChannel(RedisConnection connection, string channel, CancellationToken cancellationToken);

	/// <summary>
	/// Unsubscribe from a Redis Pub/Sub channel
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="subscriptionId">Subscription ID</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns></returns>
	Task<Result> UnsubscribeFromChannel(RedisConnection connection, string subscriptionId, CancellationToken cancellationToken);

	/// <summary>
	/// Execute a Redis transaction (MULTI/EXEC)
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="operations">List of operations to execute atomically</param>
	/// <returns>Results from each operation</returns>
	Task<Result<List<string>>> ExecuteTransaction(RedisConnection connection, List<RedisOperation> operations);

	/// <summary>
	/// Execute a Lua script in Redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="script">Lua script content</param>
	/// <param name="keys">Redis keys that the script will access</param>
	/// <param name="arguments">Arguments to pass to the script</param>
	/// <returns>Script result with hash for caching</returns>
	Task<Result<(string Result, string ScriptHash)>> ExecuteLuaScript(RedisConnection connection, string script, string[] keys, string[] arguments);
}

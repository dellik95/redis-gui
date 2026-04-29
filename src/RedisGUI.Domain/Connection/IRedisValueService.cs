using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for managing Redis value operations
/// </summary>
public interface IRedisValueService
{
	/// <summary>
	/// Get a value from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Redis value with type and TTL information</returns>
	Task<Result<RedisValue>> GetValueAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// Set a value in redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key</param>
	/// <param name="value">Value</param>
	/// <param name="ttl">Time to live</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> SetValueAsync(RedisConnection connection, string key, string value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Update a value in redis with type-specific handling
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key</param>
	/// <param name="value">Value (format depends on type)</param>
	/// <param name="type">Redis data type</param>
	/// <param name="ttl">Time to live</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> UpdateValueAsync(RedisConnection connection, string key, object value, string type, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get all keys and values from redis with pattern and pagination
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="pattern">Pattern to filter keys</param>
	/// <param name="pageSize">Number of items per page</param>
	/// <param name="pageNumber">Page number</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Paged collection of redis values</returns>
	Task<Result<PagedRedisValues>> GetAllValuesAsync(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null,
		CancellationToken cancellationToken = default);
}

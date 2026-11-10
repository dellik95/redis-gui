using RedisGUI.Domain.Redis;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for managing Redis key and value operations.
/// Also aggregates specialized Redis services for backward compatibility.
/// </summary>
public interface IRedisValueService : 
	IRedisLoadTestService, 
	IRedisSeedDataService, 
	IRedisCliService, 
	IRedisDiagnosticsService
{
	/// <summary>
	/// Get a value from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key name</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Database index</param>
	/// <returns>Retrieved Redis value</returns>
	Task<Result<RedisValue>> GetValueAsync(
		RedisConnection connection, 
		string key, 
		CancellationToken cancellationToken = default, 
		int? database = null);

	/// <summary>
	/// Set a value in redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key name</param>
	/// <param name="value">String value</param>
	/// <param name="ttl">Time to live</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Database index</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> SetValueAsync(
		RedisConnection connection, 
		string key, 
		string value, 
		TimeSpan? ttl = null, 
		CancellationToken cancellationToken = default, 
		int? database = null);

	/// <summary>
	/// Update a value in redis with type-specific handling
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key name</param>
	/// <param name="value">Object value</param>
	/// <param name="type">Redis type name</param>
	/// <param name="ttl">Time to live</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Database index</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> UpdateValueAsync(
		RedisConnection connection, 
		string key, 
		object value, 
		string type, 
		TimeSpan? ttl = null, 
		CancellationToken cancellationToken = default, 
		int? database = null);

	/// <summary>
	/// Get all keys and values from redis with pattern and pagination
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="pattern">Search pattern</param>
	/// <param name="pageSize">Page size</param>
	/// <param name="pageNumber">Page number</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Database index</param>
	/// <returns>Paged collection of Redis values</returns>
	Task<Result<PagedRedisValues>> GetAllValuesAsync(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null,
		CancellationToken cancellationToken = default,
		int? database = null);

	/// <summary>
	/// Renames an existing key
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="oldKey">Old key name</param>
	/// <param name="newKey">New key name</param>
	/// <param name="overwrite">Whether to overwrite if target exists</param>
	/// <param name="database">Database index</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> RenameKeyAsync(
		RedisConnection connection,
		string oldKey,
		string newKey,
		bool overwrite = false,
		int? database = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Duplicates an existing key with all its data and TTL intact
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="sourceKey">Source key name</param>
	/// <param name="targetKey">Target key name</param>
	/// <param name="database">Database index</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure</returns>
	Task<Result> DuplicateKeyAsync(
		RedisConnection connection,
		string sourceKey,
		string targetKey,
		int? database = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates TTL or persists multiple keys in bulk
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="keys">Keys to update</param>
	/// <param name="ttl">New TTL or null to persist</param>
	/// <param name="database">Database index</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Number of keys updated</returns>
	Task<Result<int>> BulkUpdateTtlAsync(
		RedisConnection connection,
		IEnumerable<string> keys,
		TimeSpan? ttl,
		int? database = null,
		CancellationToken cancellationToken = default);
}

using RedisGUI.Domain.Audit;
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
	/// <returns>Updated, unchanged or missing, and failed key counts.</returns>
	Task<Result<RedisBulkTtlResult>> BulkUpdateTtlAsync(
		RedisConnection connection,
		IEnumerable<string> keys,
		TimeSpan? ttl,
		int? database = null,
		CancellationToken cancellationToken = default);

	/// <summary>Retrieves a value using a Base64-encoded raw key when present.</summary>
	Task<Result<RedisValue>> GetValueByIdentityAsync(
		RedisConnection connection,
		string key,
		string? keyBase64,
		CancellationToken cancellationToken = default,
		int? database = null) =>
		GetValueAsync(connection, key, cancellationToken, database);

	/// <summary>Updates a value using a Base64-encoded raw key when present.</summary>
	Task<Result> UpdateValueByIdentityAsync(
		RedisConnection connection,
		string key,
		string? keyBase64,
		object value,
		string type,
		TimeSpan? ttl = null,
		CancellationToken cancellationToken = default,
		int? database = null) =>
		UpdateValueAsync(connection, key, value, type, ttl, cancellationToken, database);

	/// <summary>Renames a key using Base64-encoded raw keys when present.</summary>
	Task<Result> RenameKeyByIdentityAsync(
		RedisConnection connection,
		string oldKey,
		string? oldKeyBase64,
		string newKey,
		string? newKeyBase64 = null,
		bool overwrite = false,
		int? database = null,
		CancellationToken cancellationToken = default) =>
		RenameKeyAsync(connection, oldKey, newKey, overwrite, database, cancellationToken);

	/// <summary>Duplicates a key using Base64-encoded raw keys when present.</summary>
	Task<Result> DuplicateKeyByIdentityAsync(
		RedisConnection connection,
		string sourceKey,
		string? sourceKeyBase64,
		string targetKey,
		string? targetKeyBase64 = null,
		int? database = null,
		CancellationToken cancellationToken = default) =>
		DuplicateKeyAsync(connection, sourceKey, targetKey, database, cancellationToken);

	/// <summary>Bulk updates TTL using Base64-encoded raw keys when present.</summary>
	Task<Result<RedisBulkTtlResult>> BulkUpdateTtlByIdentityAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		IReadOnlyList<string?>? keysBase64,
		TimeSpan? ttl,
		int? database = null,
		CancellationToken cancellationToken = default) =>
		BulkUpdateTtlAsync(connection, keys, ttl, database, cancellationToken);
}

/// <summary>Outcome of a bounded bulk TTL operation.</summary>
public sealed record RedisBulkTtlResult(int UpdatedCount, int UnchangedCount, IReadOnlyList<string> FailedKeys) : IAuditOutcome
{
	public string FormatAuditOutcome() =>
		$"Updated TTL: {UpdatedCount}; unchanged or missing: {UnchangedCount}; failed: {FailedKeys.Count}.";
}

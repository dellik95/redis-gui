namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service for managing Redis key operations
/// </summary>
public interface IRedisKeyService
{
	/// <summary>Scans one Redis cursor page in the requested database.</summary>
	Task<Result<RedisKeyScanPage>> ScanKeysAsync(RedisConnection connection, string pattern, int database,
		string? cursor, int count, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all keys from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Target database index, or the connection default when omitted.</param>
	/// <returns>Collection of key names</returns>
	ValueTask<Result<IEnumerable<string>>> GetKeysAsync(RedisConnection connection, CancellationToken cancellationToken = default, int? database = null);

	/// <summary>
	/// Delete a single key from redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Target database index</param>
	/// <returns>True if deleted, false if key didn't exist</returns>
	Task<Result<bool>> DeleteKeyAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default, int? database = null);

	/// <summary>
	/// Delete multiple keys from redis by their exact names
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="keys">Keys to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Target database index</param>
	/// <returns>Number of keys deleted</returns>
	Task<Result<long>> DeleteKeysAsync(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default, int? database = null);

	/// <summary>
	/// Delete multiple keys from redis by their exact names with detailed results
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="keys">Keys to delete</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Target database index</param>
	/// <returns>Tuple with deleted count and list of failed keys</returns>
	Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeysAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default,
		int? database = null);

	/// <summary>
	/// Delete keys from redis matching a pattern
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="pattern">Pattern to match keys (e.g., "user:*")</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Target database index</param>
	/// <param name="onProgress">Optional callback for the number of confirmed deletions.</param>
	/// <returns>Number of keys deleted</returns>
	Task<Result<int>> DeleteKeysByPatternAsync(RedisConnection connection, string pattern, CancellationToken cancellationToken = default,
		int? database = null, Func<int, Task>? onProgress = null);

	/// <summary>
	/// Get the type of key in redis
	/// </summary>
	/// <param name="connection">Redis connection</param>
	/// <param name="key">Key to get the type of</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="database">Target database index</param>
	/// <returns>Type name (String, List, Set, Hash, SortedSet, Stream)</returns>
	Task<Result<string>> GetKeyTypeAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default, int? database = null);

	/// <summary>Deletes a key identified by raw key bytes or UTF-8 string.</summary>
	Task<Result<bool>> DeleteKeyByIdentityAsync(RedisConnection connection, string key, string? keyBase64,
		CancellationToken cancellationToken = default, int? database = null) =>
		DeleteKeyAsync(connection, key, cancellationToken, database);

	/// <summary>Deletes multiple keys identified by raw key bytes or UTF-8 strings.</summary>
	Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeysByIdentityAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		IReadOnlyList<string?>? keysBase64 = null,
		CancellationToken cancellationToken = default,
		int? database = null) =>
		DeleteMultipleKeysAsync(connection, keys, cancellationToken, database);
}

using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Legacy service for managing Redis connections and operations.
/// Delegates to specialized services for actual functionality.
/// </summary>
public class ConnectionService : IConnectionService
{
	private readonly IRedisConnectionService connectionService;
	private readonly IRedisKeyService keyService;
	private readonly IRedisValueService valueService;
	private readonly IRedisPubSubService pubSubService;
	private readonly IRedisTransactionService transactionService;
	private readonly IRedisScriptService scriptService;
	private readonly ILogger<ConnectionService> logger;

	/// <summary>
	/// Create new instance of <see cref="ConnectionService"/>
	/// </summary>
	public ConnectionService(
		IRedisConnectionService connectionService,
		IRedisKeyService keyService,
		IRedisValueService valueService,
		IRedisPubSubService pubSubService,
		IRedisTransactionService transactionService,
		IRedisScriptService scriptService,
		ILogger<ConnectionService> logger)
	{
		this.connectionService = connectionService;
		this.keyService = keyService;
		this.valueService = valueService;
		this.pubSubService = pubSubService;
		this.transactionService = transactionService;
		this.scriptService = scriptService;
		this.logger = logger;
	}

	/// <summary>
	/// Verifies if a Redis connection can be established
	/// </summary>
	public async Task<Result> CheckConnection(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		return await connectionService.CheckConnectionAsync(connection, cancellationToken);
	}

	/// <summary>
	/// Retrieves all keys from the Redis database
	/// </summary>
	public async ValueTask<Result<IEnumerable<string>>> GetKeys(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		return await keyService.GetKeysAsync(connection, cancellationToken);
	}

	/// <summary>
	/// Deletes multiple keys from the Redis database
	/// </summary>
	/// <returns>Number of keys deleted</returns>
	public async Task<Result<long>> DeleteKeys(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default)
	{
		return await keyService.DeleteKeysAsync(connection, keys, cancellationToken);
	}

	/// <summary>
	/// Sets a value for a specific key in Redis
	/// </summary>
	public async Task<Result> SetValue(RedisConnection connection, string key, string value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
	{
		return await valueService.SetValueAsync(connection, key, value, ttl, cancellationToken);
	}

	/// <summary>
	/// Retrieves a value for a specific key from Redis
	/// </summary>
	public async Task<Result<RedisValue>> GetValue(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		return await valueService.GetValueAsync(connection, key, cancellationToken);
	}

	/// <summary>
	/// Retrieves all values from Redis with optional pagination
	/// </summary>
	public async Task<Result<PagedRedisValues>> GetAllValues(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null,
		CancellationToken cancellationToken = default)
	{
		return await valueService.GetAllValuesAsync(connection, pattern, pageSize, pageNumber, cancellationToken);
	}

	/// <summary>
	/// Deletes a specific key from Redis
	/// </summary>
	public async Task<Result<bool>> DeleteKey(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		return await keyService.DeleteKeyAsync(connection, key, cancellationToken);
	}

	/// <summary>
	/// Delete multiple keys from redis by their exact names
	/// </summary>
	public async Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeys(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default)
	{
		return await keyService.DeleteMultipleKeysAsync(connection, keys, cancellationToken);
	}

	/// <summary>
	/// Delete keys from redis matching a pattern
	/// </summary>
	public async Task<Result<int>> DeleteKeysByPattern(RedisConnection connection, string pattern, CancellationToken cancellationToken = default)
	{
		return await keyService.DeleteKeysByPatternAsync(connection, pattern, cancellationToken);
	}

	/// <summary>
	/// Gets the type of value stored at a specific key
	/// </summary>
	public async Task<Result<string>> GetKeyType(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		return await keyService.GetKeyTypeAsync(connection, key, cancellationToken);
	}

	/// <summary>
	/// Updates a Redis value with type-specific logic
	/// </summary>
	public async Task<Result> UpdateValue(RedisConnection connection, string key, object value, string type, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
	{
		return await valueService.UpdateValueAsync(connection, key, value, type, ttl, cancellationToken);
	}

	/// <summary>
	/// Publishes a message to a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result> PublishMessage(RedisConnection connection, string channel, string message, CancellationToken cancellationToken = default)
	{
		return await pubSubService.PublishMessageAsync(connection, channel, message, cancellationToken);
	}

	/// <summary>
	/// Subscribes to a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result<string>> SubscribeToChannel(
		RedisConnection connection,
		string channel,
		CancellationToken cancellationToken = default)
	{
		return await pubSubService.SubscribeToChannelAsync(connection, channel, cancellationToken);
	}

	/// <summary>
	/// Unsubscribes from a Redis Pub/Sub channel
	/// </summary>
	public async Task<Result> UnsubscribeFromChannel(
		RedisConnection connection,
		string subscriptionId,
		CancellationToken cancellationToken = default)
	{
		return await pubSubService.UnsubscribeFromChannelAsync(connection, subscriptionId, cancellationToken);
	}

	/// <summary>
	/// Executes a Redis transaction (MULTI/EXEC)
	/// </summary>
	public async Task<Result<List<string>>> ExecuteTransaction(
		RedisConnection connection,
		List<RedisOperation> operations,
		CancellationToken cancellationToken = default)
	{
		return await transactionService.ExecuteTransactionAsync(connection, operations, cancellationToken);
	}

	/// <summary>
	/// Executes a Lua script in Redis using EVAL with SHA-based caching
	/// </summary>
	public async Task<Result<(string Result, string ScriptHash)>> ExecuteLuaScript(
		RedisConnection connection,
		string script,
		string[] keys,
		string[] arguments,
		CancellationToken cancellationToken = default)
	{
		return await scriptService.ExecuteLuaScriptAsync(connection, script, keys, arguments, cancellationToken);
	}
}

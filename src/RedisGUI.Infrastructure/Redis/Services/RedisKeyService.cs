using RedisGUI.Domain.Errors;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service for managing Redis key operations
/// </summary>
public class RedisKeyService : IRedisKeyService
{
	private readonly IRedisConnectionManager connectionManager;
	private readonly ILogger<RedisKeyService> logger;

	public RedisKeyService(
		IRedisConnectionManager connectionManager,
		ILogger<RedisKeyService> logger)
	{
		this.connectionManager = connectionManager;
		this.logger = logger;
	}

	/// <inheritdoc />
	public async Task<Result<RedisKeyScanPage>> ScanKeysAsync(RedisConnection connection, string pattern, int database,
		string? cursor, int count, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		Result validation = RedisKeyScan.ValidateParameters(pattern, database, cursor, count);
		if (validation.IsFailure)
		{
			return Result.Failure<RedisKeyScanPage>(validation.Error);
		}

		return await GetConnectionAsync(connection, cancellationToken)
			.BindAsync(multiplexer => ScanKeysAsync(multiplexer, connection, pattern, database, cursor, count, cancellationToken));
	}

	private async Task<Result<RedisKeyScanPage>> ScanKeysAsync(ConnectionMultiplexer multiplexer, RedisConnection connection,
		string pattern, int database, string? cursor, int count, CancellationToken cancellationToken)
	{
		Result<RedisKeyScan.Context> context = RedisKeyScan.CreateContext(multiplexer, connection.Id, pattern, database, cursor);
		return await context.BindAsync(async scan =>
		{
			try
			{
				return await RedisKeyScan.ReadPageAsync(multiplexer, scan, count, cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Redis key scan failed");
				return Result.Failure<RedisKeyScanPage>(new Error("Redis.ScanFailed", "Unable to scan Redis keys. Check the connection and command permissions."));
			}
		});
	}

	private Task<Result<ConnectionMultiplexer>> GetConnectionAsync(RedisConnection connection, CancellationToken cancellationToken) =>
		connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);

	/// <summary>
	/// Retrieves all keys from the Redis database
	/// </summary>
	public async ValueTask<Result<IEnumerable<string>>> GetKeysAsync(RedisConnection connection, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for keys: {Error}", multiplexerResult.Error.Message);
			return Result.Failure<IEnumerable<string>>(multiplexerResult.Error);
		}

		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		IEnumerable<IServer> servers = multiplexer.GetServers().Where(s => s is { IsConnected: true, Features.Scan: true });
		List<string> keys = [];

		var result = servers
			.Select(server => server.Keys(database ?? connection.DatabaseNumber.Value).Select(x => x.ToString()))
			.Aggregate(keys, (current, serverKeys) => current.Union(serverKeys).ToList());

		return Result.Success<IEnumerable<string>>(result);
	}

	/// <summary>
	/// Deletes a specific key from Redis
	/// </summary>
	public async Task<Result<bool>> DeleteKeyAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<bool>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		bool deleted = await db.KeyDeleteAsync(key);

		return Result.Success(deleted);
	}

	/// <summary>
	/// Deletes multiple keys from the Redis database
	/// </summary>
	public async Task<Result<long>> DeleteKeysAsync(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for delete keys: {Error}", multiplexerResult.Error.Message);
			return Result.Failure<long>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		RedisKey[] redisKeys = keys.Select(x => new RedisKey(x)).ToArray();
		long deletedCount = await db.KeyDeleteAsync(redisKeys);

		return Result.Success(deletedCount);
	}

	/// <summary>
	/// Delete multiple keys from redis by their exact names with detailed results
	/// </summary>
	public async Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeysAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<(int, IReadOnlyList<string>)>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		List<string> failedKeys = [];
		int deletedCount = 0;

		foreach (string key in keys)
		{
			try
			{
				bool deleted = await db.KeyDeleteAsync(key);
				if (deleted)
				{
					deletedCount++;
				}
				else
				{
					failedKeys.Add(key);
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to delete key {Key}", key);
				failedKeys.Add(key);
			}
		}

		return Result.Success((deletedCount, (IReadOnlyList<string>)failedKeys));
	}

	/// <summary>
	/// Delete keys from redis matching a pattern
	/// </summary>
	public async Task<Result<int>> DeleteKeysByPatternAsync(RedisConnection connection, string pattern, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<int>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		IDatabase db = multiplexer.GetDatabase(targetDb);
		try
		{
			IServer server = multiplexer.GetConnectedServer(connection);
			RedisKey[] keys = server.Keys(targetDb, pattern).ToArray();

			if (keys.Length == 0)
			{
				return Result.Success(0);
			}

			long deletedCount = await db.KeyDeleteAsync(keys);
			return Result.Success((int)deletedCount);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to delete keys by pattern {Pattern}", pattern);
			return Result.Failure<int>(DomainErrors.Redis.DeleteByPatternFailed);
		}
	}

	/// <summary>
	/// Gets the type of value stored at a specific key
	/// </summary>
	public async Task<Result<string>> GetKeyTypeAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<string>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		RedisType type = await db.KeyTypeAsync(key);

		return Result.Success(type.ToString());
	}
}

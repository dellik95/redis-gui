using RedisGUI.Domain.Errors;
using StackExchange.Redis;
using System.Linq;
using System.Text.Json;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service for managing Redis key operations
/// </summary>
public class RedisKeyService : IRedisKeyService
{
	private sealed record ScanCursor(Guid ConnectionId, int Database, string Pattern, string[] Nodes, int NodeIndex, string Cursor);

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
		if (database < 0 || database > 15 || count < 1 || count > 500 || string.IsNullOrEmpty(pattern) || pattern.Length > 1024 || cursor?.Length > 8192)
			return Result.Failure<RedisKeyScanPage>(new Error("Redis.InvalidScan", "Invalid scan parameters."));

		var connectionResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (connectionResult.IsFailure) return Result.Failure<RedisKeyScanPage>(connectionResult.Error);
		var multiplexer = connectionResult.Value;
		var servers = multiplexer.GetServers().Where(s => !s.IsReplica && s.ServerType != ServerType.Sentinel)
			.OrderBy(s => s.EndPoint.ToString(), StringComparer.Ordinal).ToArray();
		if (servers.Length == 0 || servers.Any(s => !s.IsConnected || !s.Features.Scan))
			return Result.Failure<RedisKeyScanPage>(new Error("Redis.ScanUnavailable", "A primary Redis server is unavailable or does not support SCAN."));
		if (servers.Length > 1 && servers.Any(s => s.ServerType != ServerType.Cluster))
			return Result.Failure<RedisKeyScanPage>(new Error("Redis.ScanUnavailable", "Multiple standalone primaries cannot be scanned reliably through one connection."));
		var nodes = servers.Select(s => s.EndPoint.ToString()!).ToArray();
		ScanCursor state;
		try
		{
			state = cursor is null ? new(connection.Id, database, pattern, nodes, 0, "0") :
				JsonSerializer.Deserialize<ScanCursor>(Convert.FromBase64String(cursor))!;
			if (state is null || state.ConnectionId != connection.Id || state.Database != database || state.Pattern != pattern ||
				state.Nodes is null || !state.Nodes.SequenceEqual(nodes) || state.NodeIndex < 0 || state.NodeIndex >= servers.Length ||
				!ulong.TryParse(state.Cursor, out _))
				return Result.Failure<RedisKeyScanPage>(new Error("Redis.InvalidCursor", "Scan cursor is invalid or the Redis topology changed. Refresh to start again."));
		}
		catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
		{
			return Result.Failure<RedisKeyScanPage>(new Error("Redis.InvalidCursor", "Scan cursor is invalid. Refresh to start again."));
		}

		try
		{
			var server = servers[state.NodeIndex];
			if (server.ServerType == ServerType.Cluster && database != 0)
				return Result.Failure<RedisKeyScanPage>(new Error("Redis.InvalidDatabase", "Redis Cluster supports database 0 only."));
			object[] args = [state.Cursor, "MATCH", pattern, "COUNT", count];
			// Cluster SCAN must target the selected primary; standalone SCAN must use the selected database.
			RedisResult raw = server.ServerType == ServerType.Cluster
				? await server.ExecuteAsync("SCAN", args).WaitAsync(cancellationToken)
				: await multiplexer.GetDatabase(database).ExecuteAsync("SCAN", args, CommandFlags.DemandMaster).WaitAsync(cancellationToken);
			RedisResult[] parts = (RedisResult[])raw!;
			string next = (string)parts[0]!;
			RedisResult[] scanned = (RedisResult[])parts[1]!;
			IDatabase db = multiplexer.GetDatabase(database);
			var metadata = await Task.WhenAll(scanned.Select(async item =>
			{
				string key = (string)item!;
				var typeTask = db.KeyTypeAsync(key);
				var ttlTask = db.KeyTimeToLiveAsync(key);
				await Task.WhenAll(typeTask, ttlTask).WaitAsync(cancellationToken);
				return typeTask.Result == RedisType.None ? null : new RedisKeyScanItem(key, typeTask.Result.ToString(), ttlTask.Result?.TotalSeconds);
			}));
			int node = state.NodeIndex;
			if (next == "0") node++;
			string? nextCursor = node >= servers.Length ? null : Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
				new ScanCursor(connection.Id, database, pattern, nodes, node, next)));
			return Result.Success(new RedisKeyScanPage(metadata.Where(x => x is not null).Select(x => x!).ToArray(), nextCursor));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Redis key scan failed");
			return Result.Failure<RedisKeyScanPage>(new Error("Redis.ScanFailed", "Unable to scan Redis keys. Check the connection and command permissions."));
		}
	}

	/// <summary>
	/// Retrieves all keys from the Redis database
	/// </summary>
	public async ValueTask<Result<IEnumerable<string>>> GetKeysAsync(RedisConnection connection, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
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
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
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
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
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
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
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
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
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
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
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

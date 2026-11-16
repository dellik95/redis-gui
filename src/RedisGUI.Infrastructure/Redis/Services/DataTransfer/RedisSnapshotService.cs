using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Captures and restores binary Redis DUMP payloads.</summary>
internal sealed class RedisSnapshotService(IRedisConnectionManager connections, ILogger<RedisSnapshotService> logger) : IRedisSnapshotService
{
	private const string CaptureScript = "local d=redis.call('DUMP',KEYS[1]); if not d then return nil end; return {redis.call('TYPE',KEYS[1]).ok,d,redis.call('PTTL',KEYS[1])}";

	public async Task<Result<bool>> SetLegacyStringAsync(RedisConnection connection, string key, string value, TimeSpan? ttl, bool overwrite,
		int database, CancellationToken cancellationToken = default)
	{
		if (connection.IsReadOnly) return Result.Failure<bool>(new Error("Connection.ReadOnlyMode", "Connection is in Safe Mode (Read-Only)."));
		var connected = await connections.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (connected.IsFailure) return Result.Failure<bool>(connected.Error);
		try
		{
			return Result.Success(await connected.Value.GetDatabase(database)
				.StringSetAsync(key, value, ttl, overwrite ? When.Always : When.NotExists).WaitAsync(cancellationToken));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to import legacy Redis string");
			return Result.Failure<bool>(new Error("Import.RestoreFailed", "Could not import the legacy string."));
		}
	}

	public async Task<Result<RedisKeySnapshot>> CaptureAsync(RedisConnection connection, byte[] key, int database, CancellationToken cancellationToken = default)
	{
		var connected = await connections.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (connected.IsFailure) return Result.Failure<RedisKeySnapshot>(connected.Error);
		try
		{
			RedisResult result = await connected.Value.GetDatabase(database)
				.ScriptEvaluateAsync(CaptureScript, [(RedisKey)key]).WaitAsync(cancellationToken);
			if (result.IsNull) return Result.Failure<RedisKeySnapshot>(new Error("Redis.KeyNotFound", "A scanned key disappeared during export."));
			var fields = (RedisResult[])result!;
			long ttl = (long)fields[2];
			return Result.Success(new RedisKeySnapshot((byte[])fields[1]!, (string)fields[0]!, ttl < 0 ? null : Math.Max(1, ttl)));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to capture Redis snapshot");
			return Result.Failure<RedisKeySnapshot>(new Error("Export.CaptureFailed", "Could not capture a key. Check Redis DUMP and EVAL permissions."));
		}
	}

	public async Task<Result<bool>> RestoreAsync(RedisConnection connection, byte[] key, byte[] payload, TimeSpan? ttl, bool overwrite,
		int database, CancellationToken cancellationToken = default)
	{
		if (connection.IsReadOnly) return Result.Failure<bool>(new Error("Connection.ReadOnlyMode", "Connection is in Safe Mode (Read-Only)."));
		var connected = await connections.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (connected.IsFailure) return Result.Failure<bool>(connected.Error);
		try
		{
			var db = connected.Value.GetDatabase(database);
			if (overwrite)
			{
				object[] args = [(RedisKey)key, (long)(ttl?.TotalMilliseconds ?? 0), payload, "REPLACE"];
				await db.ExecuteAsync("RESTORE", args).WaitAsync(cancellationToken);
			}
			else
			{
				await db.KeyRestoreAsync((RedisKey)key, payload, ttl).WaitAsync(cancellationToken);
			}
			return Result.Success(true);
		}
		catch (RedisServerException ex) when (!overwrite && ex.Message.StartsWith("BUSYKEY", StringComparison.OrdinalIgnoreCase))
		{
			return Result.Success(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to restore Redis snapshot");
			return Result.Failure<bool>(new Error("Import.RestoreFailed", "Could not restore key. The dump may be incompatible or Redis may deny RESTORE."));
		}
	}
}

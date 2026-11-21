using FluentResults;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Captures and restores binary Redis DUMP payloads.</summary>
internal sealed class RedisSnapshotService(
	IRedisConnectionManager connections,
	ILogger<RedisSnapshotService> logger,
	IRedisTopologyRegistry? topology = null) : IRedisSnapshotService
{
	private const string CaptureScript = "local d=redis.call('DUMP',KEYS[1]); if not d then return nil end; return {redis.call('TYPE',KEYS[1]).ok,d,redis.call('PTTL',KEYS[1])}";

	public async Task<Result<bool>> SetLegacyStringAsync(RedisConnection connection, string key, string value, TimeSpan? ttl, bool overwrite,
		int database, CancellationToken cancellationToken = default)
	{
		if (connection.IsReadOnly) return Result.Fail<bool>(new DomainError("Connection.ReadOnlyMode", "Connection is in Safe Mode (Read-Only)."));
		var connected = await connections.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (connected.IsFailed) return Result.Fail<bool>(connected.Errors);
		topology?.SetMode(connection.Id, connected.Value.DetectServerMode());

		var dbValidation = connected.Value.ValidateDatabase(database);
		if (dbValidation.IsFailed) return Result.Fail<bool>(dbValidation.Errors);

		try
		{
			return Result.Ok(await connected.Value.GetDatabase(database)
				.StringSetAsync(key, value, ttl, overwrite ? When.Always : When.NotExists).WaitAsync(cancellationToken));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to import legacy Redis string");
			return Result.Fail<bool>(new DomainError("Import.RestoreFailed", "Could not import the legacy string."));
		}
	}

	public async Task<Result<RedisKeySnapshot>> CaptureAsync(RedisConnection connection, byte[] key, int database, CancellationToken cancellationToken = default)
	{
		var connected = await connections.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (connected.IsFailed) return Result.Fail<RedisKeySnapshot>(connected.Errors);
		topology?.SetMode(connection.Id, connected.Value.DetectServerMode());

		var dbValidation = connected.Value.ValidateDatabase(database);
		if (dbValidation.IsFailed) return Result.Fail<RedisKeySnapshot>(dbValidation.Errors);

		var db = connected.Value.GetDatabase(database);
		try
		{
			RedisResult result = await db
				.ScriptEvaluateAsync(CaptureScript, [(RedisKey)key]).WaitAsync(cancellationToken);
			if (result.IsNull) return Result.Fail<RedisKeySnapshot>(new DomainError("Redis.KeyNotFound", "A scanned key disappeared during export."));
			var fields = (RedisResult[])result!;
			long ttl = (long)fields[2];
			return Result.Ok(new RedisKeySnapshot((byte[])fields[1]!, (string)fields[0]!, ttl < 0 ? null : Math.Max(1, ttl)));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (RedisServerException ex) when (ex.Message.Contains("NOPERM", StringComparison.OrdinalIgnoreCase) ||
			ex.Message.Contains("unknown command", StringComparison.OrdinalIgnoreCase))
		{
			// Fallback when EVAL is disabled by Redis ACLs or server config.
			return await CaptureWithoutEvalAsync(db, key, cancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to capture Redis snapshot");
			return Result.Fail<RedisKeySnapshot>(new DomainError("Export.CaptureFailed", "Could not capture a key. Check Redis DUMP and EVAL permissions."));
		}
	}

	private async Task<Result<RedisKeySnapshot>> CaptureWithoutEvalAsync(IDatabase db, byte[] key, CancellationToken cancellationToken)
	{
		try
		{
			RedisKey redisKey = key;
			var type = await db.KeyTypeAsync(redisKey).WaitAsync(cancellationToken);
			if (type == RedisType.None)
			{
				return Result.Fail<RedisKeySnapshot>(new DomainError("Redis.KeyNotFound", "A scanned key disappeared during export."));
			}

			var dumped = await db.KeyDumpAsync(redisKey).WaitAsync(cancellationToken);
			if (dumped is null)
			{
				return Result.Fail<RedisKeySnapshot>(new DomainError("Redis.KeyNotFound", "A scanned key disappeared during export."));
			}

			var ttl = await db.KeyTimeToLiveAsync(redisKey).WaitAsync(cancellationToken);
			long? ttlMs = ttl.HasValue ? Math.Max(1, (long)Math.Ceiling(ttl.Value.TotalMilliseconds)) : null;
			return Result.Ok(new RedisKeySnapshot(dumped, type.ToString().ToLowerInvariant(), ttlMs));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to capture Redis snapshot without EVAL");
			return Result.Fail<RedisKeySnapshot>(new DomainError("Export.CaptureFailed", "Could not capture a key. Check Redis DUMP permissions."));
		}
	}

	public async Task<Result<bool>> RestoreAsync(RedisConnection connection, byte[] key, byte[] payload, TimeSpan? ttl, bool overwrite,
		int database, CancellationToken cancellationToken = default)
	{
		if (connection.IsReadOnly) return Result.Fail<bool>(new DomainError("Connection.ReadOnlyMode", "Connection is in Safe Mode (Read-Only)."));
		var connected = await connections.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (connected.IsFailed) return Result.Fail<bool>(connected.Errors);
		topology?.SetMode(connection.Id, connected.Value.DetectServerMode());

		var dbValidation = connected.Value.ValidateDatabase(database);
		if (dbValidation.IsFailed) return Result.Fail<bool>(dbValidation.Errors);

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
			return Result.Ok(true);
		}
		catch (RedisServerException ex) when (!overwrite && ex.Message.StartsWith("BUSYKEY", StringComparison.OrdinalIgnoreCase))
		{
			return Result.Ok(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to restore Redis snapshot");
			return Result.Fail<bool>(new DomainError("Import.RestoreFailed", "Could not restore key. The dump may be incompatible or Redis may deny RESTORE."));
		}
	}
}

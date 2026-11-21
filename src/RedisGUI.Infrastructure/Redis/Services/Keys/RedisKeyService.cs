using FluentResults;
using RedisGUI.Domain.Errors;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service for managing Redis key operations across standalone and Redis Cluster deployments.
/// </summary>
public class RedisKeyService(
	IRedisConnectionManager connectionManager,
	ILogger<RedisKeyService> logger,
	IRedisTopologyRegistry? topology = null) : IRedisKeyService
{
	/// <inheritdoc />
	public async Task<Result<RedisKeyScanPage>> ScanKeysAsync(RedisConnection connection, string pattern, int database,
		string? cursor, int count, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		Result validation = RedisKeyScan.ValidateParameters(pattern, database, cursor, count);
		if (validation.IsFailed)
		{
			return Result.Fail<RedisKeyScanPage>(validation.Errors);
		}

		return await GetConnectionAsync(connection, cancellationToken)
			.BindAsync(multiplexer => ScanKeysAsync(multiplexer, connection, pattern, database, cursor, count, cancellationToken));
	}

	private async Task<Result<RedisKeyScanPage>> ScanKeysAsync(ConnectionMultiplexer multiplexer, RedisConnection connection,
		string pattern, int database, string? cursor, int count, CancellationToken cancellationToken)
	{
		try
		{
			topology?.SetMode(connection.Id, multiplexer.DetectServerMode());
			var context = await RedisKeyScan.CreateContextAsync(multiplexer, connection.Id, pattern, database, cursor, cancellationToken);
			return await context.BindAsync(scan => RedisKeyScan.ReadPageAsync(multiplexer, scan, count, cancellationToken));
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Redis key scan failed");
			return Result.Fail<RedisKeyScanPage>(new DomainError("Redis.ScanFailed", "Unable to scan Redis keys. Check the connection and command permissions."));
		}
	}

	private Task<Result<ConnectionMultiplexer>> GetConnectionAsync(RedisConnection connection, CancellationToken cancellationToken) =>
		connectionManager.GetConnectionAsync(connection, topology, cancellationToken);

	/// <summary>
	/// Retrieves all keys from the Redis database across primary nodes.
	/// </summary>
	public async ValueTask<Result<IEnumerable<string>>> GetKeysAsync(RedisConnection connection, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			logger.LogError("Failed to get connection for keys: {Error}", multiplexerResult.GetDomainError().Message);
			return Result.Fail<IEnumerable<string>>(multiplexerResult.Errors);
		}

		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexer.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed)
		{
			return Result.Fail<IEnumerable<string>>(dbValidation.Errors);
		}

		IServer[] servers = multiplexer.GetConnectedPrimaryServers();
		if (servers.Length == 0)
		{
			servers = multiplexer.GetServers().Where(s => s is { IsConnected: true, Features.Scan: true }).ToArray();
		}

		var distinctKeys = new HashSet<string>(StringComparer.Ordinal);
		foreach (var server in servers.Where(s => s.Features.Scan))
		{
			await foreach (var k in server.KeysAsync(targetDb).WithCancellation(cancellationToken))
			{
				distinctKeys.Add(k.ToString());
			}
		}

		return Result.Ok<IEnumerable<string>>(distinctKeys);
	}

	/// <inheritdoc />
	public Task<Result<bool>> DeleteKeyAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default, int? database = null) =>
		DeleteKeyByIdentityAsync(connection, key, null, cancellationToken, database);

	/// <inheritdoc />
	public async Task<Result<bool>> DeleteKeyByIdentityAsync(RedisConnection connection, string key, string? keyBase64,
		CancellationToken cancellationToken = default, int? database = null)
	{
		var resolvedKey = RedisServerResolver.ResolveKey(key, keyBase64);
		if (resolvedKey.IsFailed) return Result.Fail<bool>(resolvedKey.Errors);

		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			return Result.Fail<bool>(multiplexerResult.Errors);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return Result.Fail<bool>(dbValidation.Errors);

		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		bool deleted = await db.KeyDeleteAsync(resolvedKey.Value).WaitAsync(cancellationToken);

		return Result.Ok(deleted);
	}

	/// <summary>
	/// Deletes multiple keys from the Redis database (slot-grouped on Cluster to avoid CROSSSLOT errors).
	/// </summary>
	public async Task<Result<long>> DeleteKeysAsync(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			logger.LogError("Failed to get connection for delete keys: {Error}", multiplexerResult.GetDomainError().Message);
			return Result.Fail<long>(multiplexerResult.Errors);
		}

		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexer.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return Result.Fail<long>(dbValidation.Errors);

		IDatabase db = multiplexer.GetDatabase(targetDb);
		RedisKey[] redisKeys = keys.Select(x => new RedisKey(x)).ToArray();
		if (redisKeys.Length == 0)
		{
			return Result.Ok(0L);
		}

		if (multiplexer.IsCluster())
		{
			long totalDeleted = 0;
			foreach (var slotGroup in redisKeys.GroupBy(k => multiplexer.GetHashSlot(k)))
			{
				cancellationToken.ThrowIfCancellationRequested();
				totalDeleted += await db.KeyDeleteAsync(slotGroup.ToArray()).WaitAsync(cancellationToken);
			}
			return Result.Ok(totalDeleted);
		}

		long deletedCount = await db.KeyDeleteAsync(redisKeys).WaitAsync(cancellationToken);
		return Result.Ok(deletedCount);
	}

	/// <inheritdoc />
	public Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeysAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default,
		int? database = null) =>
		DeleteMultipleKeysByIdentityAsync(connection, keys, null, cancellationToken, database);

	/// <inheritdoc />
	public async Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeysByIdentityAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		IReadOnlyList<string?>? keysBase64 = null,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			return Result.Fail<(int, IReadOnlyList<string>)>(multiplexerResult.GetDomainError());
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return Result.Fail<(int, IReadOnlyList<string>)>(dbValidation.GetDomainError());

		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		List<string> failedKeys = [];
		int deletedCount = 0;

		for (int i = 0; i < keys.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string displayKey = keys[i];
			string? rawB64 = keysBase64 is not null && i < keysBase64.Count ? keysBase64[i] : null;
			var resolved = RedisServerResolver.ResolveKey(displayKey, rawB64);
			if (resolved.IsFailed)
			{
				failedKeys.Add(displayKey);
				continue;
			}

			try
			{
				bool deleted = await db.KeyDeleteAsync(resolved.Value).WaitAsync(cancellationToken);
				if (deleted)
				{
					deletedCount++;
				}
				else
				{
					failedKeys.Add(displayKey);
				}
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to delete key {Key}", displayKey);
				failedKeys.Add(displayKey);
			}
		}

		return Result.Ok((deletedCount, (IReadOnlyList<string>)failedKeys));
	}

	/// <summary>
	/// Delete keys from redis matching a pattern.
	/// </summary>
	public async Task<Result<int>> DeleteKeysByPatternAsync(RedisConnection connection, string pattern, CancellationToken cancellationToken = default,
		int? database = null, Func<int, Task>? onProgress = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			return Result.Fail<int>(multiplexerResult.Errors);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		var dbValidation = multiplexer.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return Result.Fail<int>(dbValidation.Errors);

		IDatabase db = multiplexer.GetDatabase(targetDb);
		int deletedCount = 0;
		try
		{
			string? cursor = null;
			do
			{
				cancellationToken.ThrowIfCancellationRequested();
				var page = await ScanKeysAsync(connection, pattern, targetDb, cursor, 100, cancellationToken);
				if (page.IsFailed)
					return Result.Fail<int>(new DomainError("Redis.DeleteByPatternFailed", $"Deletion stopped after deleting {deletedCount} key(s): {page.GetDomainError().Message}"));
				foreach (var item in page.Value.Keys)
				{
					cancellationToken.ThrowIfCancellationRequested();
					RedisKey key = item.RawKey is null ? (RedisKey)item.Key : (RedisKey)item.RawKey;
					if (await db.KeyDeleteAsync(key))
					{
						deletedCount++;
						if (onProgress is not null) await onProgress(deletedCount);
					}
				}
				cursor = page.Value.NextCursor;
			} while (cursor is not null);
			return Result.Ok(deletedCount);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to delete keys by pattern {Pattern}", pattern);
			return Result.Fail<int>(new DomainError("Redis.DeleteByPatternFailed", $"Deletion stopped after deleting {deletedCount} key(s). Refresh the key list before retrying."));
		}
	}

	/// <summary>
	/// Gets the type of value stored at a specific key.
	/// </summary>
	public async Task<Result<string>> GetKeyTypeAsync(RedisConnection connection, string key, CancellationToken cancellationToken = default, int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			return Result.Fail<string>(multiplexerResult.Errors);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return Result.Fail<string>(dbValidation.Errors);

		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		RedisType type = await db.KeyTypeAsync(key).WaitAsync(cancellationToken);

		return Result.Ok(type.ToString());
	}
}

using FluentResults;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.Common;
using RedisGUI.Infrastructure.Redis.TypeHandlers;
using StackExchange.Redis;
using System.Linq;
using RedisValue = RedisGUI.Domain.Redis.RedisValue;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service responsible for managing Redis core keys and values across standalone and Cluster topologies.
/// </summary>
public class RedisValueService(
	IRedisConnectionManager connectionManager,
	IRedisKeyService keyService,
	IRedisTypeHandlerFactory typeHandlerFactory,
	IRedisLoadTestService loadTestService,
	IRedisSeedDataService seedDataService,
	IRedisCliService cliService,
	IRedisDiagnosticsService diagnosticsService,
	ILogger<RedisValueService> logger,
	IRedisTopologyRegistry? topology = null) : IRedisValueService
{
	#region IRedisLoadTestService Delegation

	/// <inheritdoc />
	public Task<Result<RedisLoadTestResult>> RunLoadTestAsync(
		RedisConnection connection,
		RedisLoadTestRequest request,
		CancellationToken cancellationToken = default) =>
		loadTestService.RunLoadTestAsync(connection, request, cancellationToken);

	/// <inheritdoc />
	public Task<Result<RedisLoadTestResult>> RunStreamingLoadTestAsync(
		RedisConnection connection,
		RedisLoadTestRequest request,
		Func<RedisLoadTestProgress, Task>? onProgress = null,
		CancellationToken cancellationToken = default) =>
		loadTestService.RunStreamingLoadTestAsync(connection, request, onProgress, cancellationToken);

	#endregion

	#region IRedisSeedDataService Delegation

	/// <inheritdoc />
	public Task<Result<SeedDataResult>> SeedSampleDataAsync(
		RedisConnection connection,
		SeedSampleDataOptions? options = null,
		CancellationToken cancellationToken = default,
		Func<int, int, Task>? onProgress = null) =>
		seedDataService.SeedSampleDataAsync(connection, options, cancellationToken, onProgress);

	#endregion

	#region IRedisCliService Delegation

	/// <inheritdoc />
	public Task<Result<string>> ExecuteCliCommandAsync(
		RedisConnection connection,
		string rawCommand,
		int? database = null,
		CancellationToken cancellationToken = default) =>
		cliService.ExecuteCliCommandAsync(connection, rawCommand, database, cancellationToken);

	#endregion

	#region IRedisDiagnosticsService Delegation

	/// <inheritdoc />
	public Task<Result<long>> GetKeyMemoryUsageAsync(
		RedisConnection connection,
		string key,
		CancellationToken cancellationToken = default,
		int? database = null,
		string? keyBase64 = null) =>
		diagnosticsService.GetKeyMemoryUsageAsync(connection, key, cancellationToken, database, keyBase64);

	/// <inheritdoc />
	public Task<Result<Dictionary<int, long>>> GetKeyspaceInfoAsync(
		RedisConnection connection,
		CancellationToken cancellationToken = default) =>
		diagnosticsService.GetKeyspaceInfoAsync(connection, cancellationToken);

	/// <inheritdoc />
	public Task<Result<List<SlowlogEntryInfo>>> GetSlowlogAsync(
		RedisConnection connection,
		int count = 25,
		CancellationToken cancellationToken = default) =>
		diagnosticsService.GetSlowlogAsync(connection, count, cancellationToken);

	/// <inheritdoc />
	public Task<Result<RedisServerInfoDto>> GetServerInfoAsync(
		RedisConnection connection,
		CancellationToken cancellationToken = default) =>
		diagnosticsService.GetServerInfoAsync(connection, cancellationToken);

	/// <inheritdoc />
	public Task<Result<RedisServerConfigDto>> GetServerConfigAsync(
		RedisConnection connection,
		string? pattern = null,
		CancellationToken cancellationToken = default) =>
		diagnosticsService.GetServerConfigAsync(connection, pattern, cancellationToken);

	/// <inheritdoc />
	public Task<Result<RedisSetConfigResponseDto>> SetServerConfigAsync(
		RedisConnection connection,
		string parameterName,
		string parameterValue,
		CancellationToken cancellationToken = default) =>
		diagnosticsService.SetServerConfigAsync(connection, parameterName, parameterValue, cancellationToken);

	#endregion

	#region Core Value Operations

	private Task<Result<ConnectionMultiplexer>> GetConnectionAsync(RedisConnection connection, CancellationToken cancellationToken) =>
		connectionManager.GetConnectionAsync(connection, topology, cancellationToken);

	/// <inheritdoc />
	public async Task<Result> SetValueAsync(
		RedisConnection connection,
		string key,
		string value,
		TimeSpan? expiry = null,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			logger.LogError("Failed to get connection for set value: {Error}", multiplexerResult.GetDomainError().Message);
			return Result.Fail(multiplexerResult.Errors);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return dbValidation;

		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		bool success = expiry.HasValue && expiry.Value.TotalSeconds > 0
			? await db.StringSetAsync(key, value, (Expiration)expiry.Value).WaitAsync(cancellationToken)
			: await db.StringSetAsync(key, value).WaitAsync(cancellationToken);

		return success ? Result.Ok() : Result.Fail(DomainErrors.Redis.UpdateFailed);
	}

	/// <inheritdoc />
	public Task<Result<RedisValue>> GetValueAsync(
		RedisConnection connection,
		string key,
		CancellationToken cancellationToken = default,
		int? database = null) =>
		GetValueByIdentityAsync(connection, key, null, cancellationToken, database);

	/// <inheritdoc />
	public async Task<Result<RedisValue>> GetValueByIdentityAsync(
		RedisConnection connection,
		string key,
		string? keyBase64,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		var resolvedKey = RedisServerResolver.ResolveKey(key, keyBase64);
		if (resolvedKey.IsFailed) return Result.Fail<RedisValue>(resolvedKey.Errors);

		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			return Result.Fail<RedisValue>(multiplexerResult.Errors);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return Result.Fail<RedisValue>(dbValidation.Errors);

		IDatabase databaseInstance = multiplexerResult.Value.GetDatabase(targetDb);

		try
		{
			RedisType type = await databaseInstance.KeyTypeAsync(resolvedKey.Value).WaitAsync(cancellationToken);
			if (type == RedisType.None)
			{
				return Result.Fail<RedisValue>(DomainErrors.Redis.KeyNotFound);
			}

			var handler = typeHandlerFactory.GetHandler(type);
			if (handler is null)
			{
				return Result.Fail<RedisValue>(DomainErrors.Redis.UnsupportedKeyType);
			}

			var valueResult = await handler.ReadRawAsync(databaseInstance, resolvedKey.Value).WaitAsync(cancellationToken);
			if (valueResult.IsFailed)
			{
				return Result.Fail<RedisValue>(valueResult.Errors);
			}

			TimeSpan? ttl = await databaseInstance.KeyTimeToLiveAsync(resolvedKey.Value).WaitAsync(cancellationToken);
			string serializedValue = handler.Serialize(valueResult.Value);
			string resolvedDisplayKey = !string.IsNullOrWhiteSpace(key)
				? key
				: resolvedKey.Value.ToString() is { Length: > 0 } decoded && !string.IsNullOrWhiteSpace(decoded)
					? decoded
					: keyBase64 ?? "(binary)";

			return RedisValue.Create(resolvedDisplayKey, serializedValue, type.ToString(), ttl);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to get value for key {Key}", key);
			return Result.Fail<RedisValue>(DomainErrors.Redis.ValueRetrievalFailed);
		}
	}

	/// <inheritdoc />
	public async Task<Result<PagedRedisValues>> GetAllValuesAsync(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		Result<IEnumerable<string>> keysResult = await keyService.GetKeysAsync(connection, cancellationToken, database);
		if (keysResult.IsFailed)
		{
			return Result.Fail<PagedRedisValues>(keysResult.Errors);
		}

		List<string> keys = keysResult.Value.ToList();

		if (!string.IsNullOrEmpty(pattern) && pattern != "*")
		{
			keys = keys.Where(k => RedisPatternMatcher.IsMatch(k, pattern)).ToList();
		}

		int totalCount = keys.Count;
		int actualPageNumber = pageNumber ?? 1;
		int actualPageSize = pageSize ?? 100;
		int skip = (actualPageNumber - 1) * actualPageSize;

		List<string> pagedKeys = keys.Skip(skip).Take(actualPageSize).ToList();

		List<RedisValue> values = [];
		foreach (string key in pagedKeys)
		{
			Result<RedisValue> valueResult = await GetValueAsync(connection, key, cancellationToken, database);
			if (valueResult.IsSuccess)
			{
				values.Add(valueResult.Value);
			}
		}

		PagedRedisValues pagedResult = PagedRedisValues.Create(values, totalCount, actualPageNumber, actualPageSize);
		return Result.Ok(pagedResult);
	}

	/// <inheritdoc />
	public Task<Result> UpdateValueAsync(
		RedisConnection connection,
		string key,
		object value,
		string type,
		TimeSpan? ttl = null,
		CancellationToken cancellationToken = default,
		int? database = null) =>
		UpdateValueByIdentityAsync(connection, key, null, value, type, ttl, cancellationToken, database);

	/// <inheritdoc />
	public async Task<Result> UpdateValueByIdentityAsync(
		RedisConnection connection,
		string key,
		string? keyBase64,
		object value,
		string type,
		TimeSpan? ttl = null,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		var resolvedKey = RedisServerResolver.ResolveKey(key, keyBase64);
		if (resolvedKey.IsFailed) return resolvedKey.ToResult();

		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			logger.LogError("Failed to get connection for update value: {Error}", multiplexerResult.GetDomainError().Message);
			return Result.Fail(multiplexerResult.Errors);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return dbValidation;

		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);

		try
		{
			var handler = typeHandlerFactory.GetHandler(type);
			if (handler is null)
			{
				return Result.Fail(DomainErrors.Redis.UnsupportedKeyType);
			}

			return await handler.WriteAsync(db, resolvedKey.Value, value, ttl).WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to update value for key {Key}", key);
			return Result.Fail(DomainErrors.Redis.ValueUpdateFailed);
		}
	}

	/// <inheritdoc />
	public Task<Result> RenameKeyAsync(
		RedisConnection connection,
		string oldKey,
		string newKey,
		bool overwrite = false,
		int? database = null,
		CancellationToken cancellationToken = default) =>
		RenameKeyByIdentityAsync(connection, oldKey, null, newKey, null, overwrite, database, cancellationToken);

	/// <inheritdoc />
	public async Task<Result> RenameKeyByIdentityAsync(
		RedisConnection connection,
		string oldKey,
		string? oldKeyBase64,
		string newKey,
		string? newKeyBase64 = null,
		bool overwrite = false,
		int? database = null,
		CancellationToken cancellationToken = default)
	{
		var resolvedOld = RedisServerResolver.ResolveKey(oldKey, oldKeyBase64);
		if (resolvedOld.IsFailed) return resolvedOld.ToResult();
		var resolvedNew = RedisServerResolver.ResolveKey(newKey, newKeyBase64);
		if (resolvedNew.IsFailed) return resolvedNew.ToResult();

		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed) return Result.Fail(multiplexerResult.Errors);

		var multiplexer = multiplexerResult.Value;
		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexer.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return dbValidation;

		var db = multiplexer.GetDatabase(targetDb);
		if (resolvedOld.Value == resolvedNew.Value)
		{
			return await db.KeyExistsAsync(resolvedOld.Value).WaitAsync(cancellationToken)
				? Result.Ok()
				: Result.Fail(DomainErrors.Redis.KeyNotFound);
		}

		// On Redis Cluster, RENAME across different hash slots fails with CROSSSLOT.
		if (multiplexer.IsCluster() && multiplexer.GetHashSlot(resolvedOld.Value) != multiplexer.GetHashSlot(resolvedNew.Value))
		{
			return await CrossSlotRenameAsync(db, resolvedOld.Value, resolvedNew.Value, newKey, overwrite, cancellationToken);
		}

		try
		{
			bool renamed = await db.KeyRenameAsync(resolvedOld.Value, resolvedNew.Value, overwrite ? When.Always : When.NotExists)
				.WaitAsync(cancellationToken);
			if (!renamed && !overwrite)
			{
				return Result.Fail(new DomainError("Storage.KeyAlreadyExists", $"Key '{newKey}' already exists."));
			}
			return Result.Ok();
		}
		catch (RedisServerException ex) when (ex.Message.Contains("no such key", StringComparison.OrdinalIgnoreCase))
		{
			return Result.Fail(DomainErrors.Redis.KeyNotFound);
		}
	}

	private async Task<Result> CrossSlotRenameAsync(
		IDatabase db,
		RedisKey oldKey,
		RedisKey newKey,
		string newKeyDisplay,
		bool overwrite,
		CancellationToken cancellationToken)
	{
		byte[]? dumped = await db.KeyDumpAsync(oldKey).WaitAsync(cancellationToken);
		if (dumped is null)
		{
			return Result.Fail(DomainErrors.Redis.KeyNotFound);
		}

		if (!overwrite && await db.KeyExistsAsync(newKey).WaitAsync(cancellationToken))
		{
			return Result.Fail(new DomainError("Storage.KeyAlreadyExists", $"Key '{newKeyDisplay}' already exists."));
		}

		TimeSpan? ttl = await db.KeyTimeToLiveAsync(oldKey).WaitAsync(cancellationToken);
		try
		{
			if (overwrite)
			{
				object[] args = [newKey, (long)(ttl?.TotalMilliseconds > 0 ? ttl.Value.TotalMilliseconds : 0), dumped, "REPLACE"];
				await db.ExecuteAsync("RESTORE", args).WaitAsync(cancellationToken);
			}
			else
			{
				await db.KeyRestoreAsync(newKey, dumped, ttl).WaitAsync(cancellationToken);
			}
		}
		catch (RedisServerException ex) when (!overwrite && ex.Message.StartsWith("BUSYKEY", StringComparison.OrdinalIgnoreCase))
		{
			return Result.Fail(new DomainError("Storage.KeyAlreadyExists", $"Key '{newKeyDisplay}' already exists."));
		}

		if (!await db.KeyDeleteAsync(oldKey).WaitAsync(cancellationToken))
		{
			logger.LogWarning("Cross-slot rename restored '{NewKey}' but could not delete source key", newKeyDisplay);
		}

		return Result.Ok();
	}

	/// <inheritdoc />
	public Task<Result> DuplicateKeyAsync(
		RedisConnection connection,
		string sourceKey,
		string targetKey,
		int? database = null,
		CancellationToken cancellationToken = default) =>
		DuplicateKeyByIdentityAsync(connection, sourceKey, null, targetKey, null, database, cancellationToken);

	/// <inheritdoc />
	public async Task<Result> DuplicateKeyByIdentityAsync(
		RedisConnection connection,
		string sourceKey,
		string? sourceKeyBase64,
		string targetKey,
		string? targetKeyBase64 = null,
		int? database = null,
		CancellationToken cancellationToken = default)
	{
		var resolvedSource = RedisServerResolver.ResolveKey(sourceKey, sourceKeyBase64);
		if (resolvedSource.IsFailed) return resolvedSource.ToResult();
		var resolvedTarget = RedisServerResolver.ResolveKey(targetKey, targetKeyBase64);
		if (resolvedTarget.IsFailed) return resolvedTarget.ToResult();

		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed) return Result.Fail(multiplexerResult.Errors);

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return dbValidation;

		var db = multiplexerResult.Value.GetDatabase(targetDb);
		byte[]? dumped = await db.KeyDumpAsync(resolvedSource.Value).WaitAsync(cancellationToken);
		if (dumped is null)
		{
			return Result.Fail(DomainErrors.Storage.ItemNotFound);
		}

		TimeSpan? ttl = await db.KeyTimeToLiveAsync(resolvedSource.Value).WaitAsync(cancellationToken);
		try
		{
			await db.KeyRestoreAsync(resolvedTarget.Value, dumped, ttl).WaitAsync(cancellationToken);
			return Result.Ok();
		}
		catch (RedisServerException ex) when (ex.Message.StartsWith("BUSYKEY", StringComparison.OrdinalIgnoreCase))
		{
			return Result.Fail(new DomainError("Storage.KeyAlreadyExists", $"Key '{targetKey}' already exists."));
		}
	}

	/// <inheritdoc />
	public Task<Result<RedisBulkTtlResult>> BulkUpdateTtlAsync(
		RedisConnection connection,
		IEnumerable<string> keys,
		TimeSpan? ttl,
		int? database = null,
		CancellationToken cancellationToken = default) =>
		BulkUpdateTtlByIdentityAsync(connection, keys as IReadOnlyList<string> ?? keys.ToArray(), null, ttl, database, cancellationToken);

	/// <inheritdoc />
	public async Task<Result<RedisBulkTtlResult>> BulkUpdateTtlByIdentityAsync(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		IReadOnlyList<string?>? keysBase64,
		TimeSpan? ttl,
		int? database = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await GetConnectionAsync(connection, cancellationToken);
		if (multiplexerResult.IsFailed) return Result.Fail<RedisBulkTtlResult>(multiplexerResult.Errors);

		int targetDb = database ?? connection.DatabaseNumber.Value;
		var dbValidation = multiplexerResult.Value.ValidateDatabase(targetDb);
		if (dbValidation.IsFailed) return Result.Fail<RedisBulkTtlResult>(dbValidation.Errors);

		var db = multiplexerResult.Value.GetDatabase(targetDb);
		int updatedCount = 0;
		int unchangedCount = 0;
		List<string> failedKeys = [];

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
				bool updated = ttl.HasValue && ttl.Value > TimeSpan.Zero
					? await db.KeyExpireAsync(resolved.Value, ttl.Value).WaitAsync(cancellationToken)
					: await db.KeyPersistAsync(resolved.Value).WaitAsync(cancellationToken);
				if (updated) updatedCount++;
				else unchangedCount++;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to update TTL for key {Key}", displayKey);
				failedKeys.Add(displayKey);
			}
		}

		return Result.Ok(new RedisBulkTtlResult(updatedCount, unchangedCount, failedKeys));
	}

	#endregion
}

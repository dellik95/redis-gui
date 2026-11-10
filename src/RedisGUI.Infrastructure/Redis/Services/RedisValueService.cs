using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.Common;
using RedisGUI.Infrastructure.Redis.TypeHandlers;
using StackExchange.Redis;
using System.Linq;
using RedisValue = RedisGUI.Domain.Redis.RedisValue;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service responsible for managing Redis core keys and values.
/// Utilizes type handler strategies for Redis data types and aggregates specialized services.
/// </summary>
public class RedisValueService(
	IConnectionPool connectionPool,
	IRedisKeyService keyService,
	IRedisTypeHandlerFactory typeHandlerFactory,
	IRedisLoadTestService loadTestService,
	IRedisSeedDataService seedDataService,
	IRedisCliService cliService,
	IRedisDiagnosticsService diagnosticsService,
	ILogger<RedisValueService> logger) : IRedisValueService
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
		CancellationToken cancellationToken = default) =>
		seedDataService.SeedSampleDataAsync(connection, options, cancellationToken);

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
		CancellationToken cancellationToken = default) =>
		diagnosticsService.GetKeyMemoryUsageAsync(connection, key, cancellationToken);

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

	/// <inheritdoc />
	public async Task<Result> SetValueAsync(
		RedisConnection connection,
		string key,
		string value,
		TimeSpan? expiry = null,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for set value: {Error}", multiplexerResult.Error.Message);
			return Result.Failure(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		bool success = expiry.HasValue && expiry.Value.TotalSeconds > 0
			? await db.StringSetAsync(key, value, (Expiration)expiry.Value)
			: await db.StringSetAsync(key, value);
		
		return success ? Result.Success() : Result.Failure(DomainErrors.Redis.UpdateFailed);
	}

	/// <inheritdoc />
	public async Task<Result<RedisValue>> GetValueAsync(
		RedisConnection connection, 
		string key, 
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<RedisValue>(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase databaseInstance = multiplexerResult.Value.GetDatabase(targetDb);
		
		try
		{
			RedisType type = await databaseInstance.KeyTypeAsync(key);
			if (type == RedisType.None)
			{
				return Result.Failure<RedisValue>(DomainErrors.Redis.KeyNotFound);
			}

			var handler = typeHandlerFactory.GetHandler(type);
			if (handler is null)
			{
				return Result.Failure<RedisValue>(DomainErrors.Redis.UnsupportedKeyType);
			}

			var valueResult = await handler.ReadRawAsync(databaseInstance, key);
			if (valueResult.IsFailure)
			{
				return Result.Failure<RedisValue>(valueResult.Error);
			}

			TimeSpan? ttl = await databaseInstance.KeyTimeToLiveAsync(key);
			string serializedValue = handler.Serialize(valueResult.Value);

			return RedisValue.Create(key, serializedValue, type.ToString(), ttl);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to get value for key {Key}", key);
			return Result.Failure<RedisValue>(DomainErrors.Redis.ValueRetrievalFailed);
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
		Result<IEnumerable<string>> keysResult = await keyService.GetKeysAsync(connection, cancellationToken);
		if (keysResult.IsFailure)
		{
			return Result.Failure<PagedRedisValues>(keysResult.Error);
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
		return Result.Success(pagedResult);
	}

	/// <inheritdoc />
	public async Task<Result> UpdateValueAsync(
		RedisConnection connection,
		string key,
		object value,
		string type,
		TimeSpan? ttl = null,
		CancellationToken cancellationToken = default,
		int? database = null)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			logger.LogError("Failed to get connection for update value: {Error}", multiplexerResult.Error.Message);
			return Result.Failure(multiplexerResult.Error);
		}

		int targetDb = database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexerResult.Value.GetDatabase(targetDb);
		
		try
		{
			var handler = typeHandlerFactory.GetHandler(type);
			if (handler is null)
			{
				return Result.Failure(DomainErrors.Redis.UnsupportedKeyType);
			}

			return await handler.WriteAsync(db, key, value, ttl);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to update value for key {Key}", key);
			return Result.Failure(DomainErrors.Redis.ValueUpdateFailed);
		}
	}

	/// <inheritdoc />
	public async Task<Result> RenameKeyAsync(
		RedisConnection connection,
		string oldKey,
		string newKey,
		bool overwrite = false,
		int? database = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure(multiplexerResult.Error);

		var db = multiplexerResult.Value.GetDatabase(database ?? connection.DatabaseNumber.Value);
		bool renamed = await db.KeyRenameAsync(oldKey, newKey, overwrite ? When.Always : When.NotExists);
		if (!renamed && !overwrite)
		{
			return Result.Failure(new Error("Storage.KeyAlreadyExists", $"Key '{newKey}' already exists."));
		}
		return Result.Success();
	}

	/// <inheritdoc />
	public async Task<Result> DuplicateKeyAsync(
		RedisConnection connection,
		string sourceKey,
		string targetKey,
		int? database = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure(multiplexerResult.Error);

		var db = multiplexerResult.Value.GetDatabase(database ?? connection.DatabaseNumber.Value);
		byte[]? dumped = await db.KeyDumpAsync(sourceKey);
		if (dumped == null)
		{
			return Result.Failure(DomainErrors.Storage.ItemNotFound);
		}
		TimeSpan? ttl = await db.KeyTimeToLiveAsync(sourceKey);
		await db.KeyRestoreAsync(targetKey, dumped, ttl);
		return Result.Success();
	}

	/// <inheritdoc />
	public async Task<Result<int>> BulkUpdateTtlAsync(
		RedisConnection connection,
		IEnumerable<string> keys,
		TimeSpan? ttl,
		int? database = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure) return Result.Failure<int>(multiplexerResult.Error);

		var db = multiplexerResult.Value.GetDatabase(database ?? connection.DatabaseNumber.Value);
		int updatedCount = 0;
		foreach (var key in keys)
		{
			if (ttl.HasValue && ttl.Value > TimeSpan.Zero)
			{
				if (await db.KeyExpireAsync(key, ttl.Value)) updatedCount++;
			}
			else
			{
				if (await db.KeyPersistAsync(key)) updatedCount++;
			}
		}
		return Result.Success(updatedCount);
	}

	#endregion
}

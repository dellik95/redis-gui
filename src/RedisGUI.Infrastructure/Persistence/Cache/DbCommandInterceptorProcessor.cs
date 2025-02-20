using System;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Persistence.InMemoryDbDataReader;
using RedisGUI.Infrastructure.Persistence.SqlProcessor;
using ZiggyCreatures.Caching.Fusion;

namespace RedisGUI.Infrastructure.Persistence.Cache;

/// <summary>
/// Processes database commands for caching operations
/// </summary>
public class DbCommandInterceptorProcessor : IDbCommandInterceptorProcessor
{
	private readonly ICacheKeyProvider cacheKeyProvider;
	private readonly ISqlCommandsProcessor sqlCommandsProcessor;
	private readonly IFusionCache cacheService;
	private readonly CacheConfiguration config;

	/// <summary>
	/// Initializes a new instance of the DbCommandInterceptorProcessor
	/// </summary>
	/// <param name="cacheKeyProvider">Provider for generating cache keys</param>
	/// <param name="sqlCommandsProcessor">Processor for SQL commands</param>
	/// <param name="cacheService">The caching service</param>
	/// <param name="options">Cache configuration options</param>
	public DbCommandInterceptorProcessor(
		ICacheKeyProvider cacheKeyProvider,
		ISqlCommandsProcessor sqlCommandsProcessor,
		IFusionCache cacheService,
		IOptions<CacheConfiguration> options)
	{
		this.cacheKeyProvider = cacheKeyProvider;
		this.sqlCommandsProcessor = sqlCommandsProcessor;
		this.cacheService = cacheService;
		this.config = options.Value;
	}

	/// <inheritdoc />
	public async ValueTask<T> ProcessExecutedCommands<T>(DbCommand command, DbContext context, T result, CancellationToken cancellationToken = default)
	{
		if (context is null || command is null || sqlCommandsProcessor.IsCrudCommand(command.CommandText))
		{
			return result;
		}

		var tableNames = sqlCommandsProcessor.GetSqlCommandTableNames(command.CommandText, context, config.CachedTables, config.IgnoredTables);
		if (tableNames == null || tableNames.Count == 0)
		{
			return result;
		}

		var efCacheKey = cacheKeyProvider.GetCacheKey(command, context, tableNames);
		if (string.IsNullOrEmpty(efCacheKey) || result is InMemoryTableRowsDataReader)
		{
			return result;
		}

		switch (result)
		{
			case int data:
				await cacheService.SetAsync(efCacheKey, new CacheEntry { NonQuery = data }, tags: tableNames, token: cancellationToken, duration: this.config.Duration);
				return result;
			case DbDataReader dataReader:
				DbTableRows tableRows;
				await using (var dbReaderLoader = new DbDataReaderLoader(dataReader))
				{
					tableRows = dbReaderLoader.Load();
				}
				await cacheService.SetAsync(efCacheKey, new CacheEntry { TableRows = tableRows }, tags: tableNames, token: cancellationToken, duration: this.config.Duration);
				return (T)(object)new InMemoryTableRowsDataReader(tableRows);
			case object:
				await cacheService.SetAsync(efCacheKey, new CacheEntry { Scalar = result }, tags: tableNames, token: cancellationToken, duration: this.config.Duration);
				return result;
			default:
				return result;
		}
	}

	/// <inheritdoc />
	public async ValueTask<T> ProcessExecutingCommands<T>(DbCommand command, DbContext context, T result, CancellationToken cancellationToken = default)
	{
		if (context is null || command is null)
		{
			return result;
		}

		var tableNames = sqlCommandsProcessor.GetSqlCommandTableNames(command.CommandText, context, config.CachedTables, config.IgnoredTables);
		if (tableNames == null || tableNames.Count == 0)
		{
			return result;
		}

		var efCacheKey = cacheKeyProvider.GetCacheKey(command, context, tableNames);
		if (string.IsNullOrEmpty(efCacheKey))
		{
			return result;
		}

		if (sqlCommandsProcessor.IsCrudCommand(command.CommandText))
		{
			await cacheService.RemoveByTagAsync(tableNames, token: cancellationToken);
			return result;
		}

		var maybeResult = await cacheService.TryGetAsync<CacheEntry>(efCacheKey, token: cancellationToken);
		if (!maybeResult.HasValue)
		{
			return result;
		}

		var cacheResult = maybeResult.Value;
		return result switch
		{
			InterceptionResult<DbDataReader> => await HandleDbDataReaderResult<T>(cacheResult),
			InterceptionResult<int> => HandleIntResult<T>(cacheResult),
			InterceptionResult<object> => HandleObjectResult<T>(cacheResult),
			_ => result
		};
	}

	private static async Task<T> HandleDbDataReaderResult<T>(CacheEntry cacheResult)
	{
		await using var dataRows = new InMemoryTableRowsDataReader(cacheResult.TableRows);
		return (T)Convert.ChangeType(InterceptionResult<DbDataReader>.SuppressWithResult(dataRows), typeof(T), CultureInfo.InvariantCulture);
	}

	private static T HandleIntResult<T>(CacheEntry cacheResult)
	{
		var cachedResult = cacheResult.IsNull ? 0 : cacheResult.NonQuery;
		return (T)Convert.ChangeType(InterceptionResult<int>.SuppressWithResult(cachedResult), typeof(T), CultureInfo.InvariantCulture);
	}

	private static T HandleObjectResult<T>(CacheEntry cacheResult)
	{
		var cachedResult = cacheResult.IsNull ? null : cacheResult.Scalar;
		return (T)Convert.ChangeType(InterceptionResult<object>.SuppressWithResult(cachedResult ?? new object()), typeof(T), CultureInfo.InvariantCulture);
	}
}

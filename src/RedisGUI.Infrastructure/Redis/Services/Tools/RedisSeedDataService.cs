using FluentResults;
using RedisGUI.Domain.Redis;
using RedisGUI.Domain.Errors;
using RedisGUI.Infrastructure.Redis.Seeding;
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service responsible for generating and populating rich, realistic sample and benchmark dataset in Redis.
/// Uses Strategy and Factory patterns for key naming and payload generation.
/// </summary>
public class RedisSeedDataService(
	IRedisConnectionManager connectionManager,
	ILogger<RedisSeedDataService> logger,
	IRedisTopologyRegistry? topology = null) : IRedisSeedDataService
{
	/// <inheritdoc />
	public async Task<Result<SeedDataResult>> SeedSampleDataAsync(
		RedisConnection connection,
		SeedSampleDataOptions? options = null,
		CancellationToken cancellationToken = default,
		Func<int, int, Task>? onProgress = null)
	{
		if (connection.IsReadOnly) return Result.Fail<SeedDataResult>(DomainErrors.Connection.ReadOnlyMode);
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailed)
		{
			return Result.Fail<SeedDataResult>(multiplexerResult.Errors);
		}

		var multiplexer = multiplexerResult.Value;
		topology?.SetMode(connection.Id, multiplexer.DetectServerMode());
		bool isCluster = multiplexer.IsCluster();

		var optionsActual = options ?? new SeedSampleDataOptions();
		int totalKeys = Math.Clamp(optionsActual.TotalKeys, 1, 10000);
		string prefix = string.IsNullOrWhiteSpace(optionsActual.CustomPrefix) ? "demo" : optionsActual.CustomPrefix.Trim().TrimEnd(':');

		// Resolve databases (Redis Cluster only supports DB 0).
		List<int> targetDbs = ResolveTargetDatabases(optionsActual, isCluster);
		if (isCluster && targetDbs.Any(db => db != 0))
		{
			return Result.Fail<SeedDataResult>(new DomainError("Redis.InvalidDatabase", "Redis Cluster supports database 0 only."));
		}

		// Resolve data types
		var requestedTypes = optionsActual.DataTypes?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.ToLowerInvariant()).ToList();
		List<string> enabledTypes = (requestedTypes != null && requestedTypes.Count > 0)
			? requestedTypes
			: ["string", "hash", "list", "set", "zset", "stream"];

		var countsByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			["string"] = 0,
			["hash"] = 0,
			["list"] = 0,
			["set"] = 0,
			["zset"] = 0,
			["stream"] = 0
		};

		var rng = new Random();
		int processedKeys = 0;
		int totalSeeded = 0;
		int failedKeys = 0;
		bool cancelled = false;
		string? firstFailure = null;
		var affectedDbs = new HashSet<int>();

		var namingStrategy = KeyNamingStrategyFactory.GetStrategy(optionsActual.KeyComplexity);

		for (int i = 0; i < totalKeys; i++)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				cancelled = true;
				break;
			}

			int dbIndex = targetDbs[i % targetDbs.Count];
			var db = multiplexer.GetDatabase(dbIndex);
			string type = enabledTypes[i % enabledTypes.Count];
			string keyName = namingStrategy.GenerateKeyName(prefix, type, i, rng);

			TimeSpan? ttl = optionsActual.TtlStrategy switch
			{
				"persistent" => null,
				"expiring" => TimeSpan.FromSeconds(rng.Next(60, 3600)),
				_ => (rng.NextDouble() < 0.4 ? TimeSpan.FromSeconds(rng.Next(60, 86400)) : null)
			};

			var seedStrategy = SeedDataStrategyFactory.GetStrategy(type);
			processedKeys++;
			try
			{
				await seedStrategy.SeedAsync(db, keyName, optionsActual.PayloadComplexity, i, ttl, rng);
				countsByType[type] = countsByType.GetValueOrDefault(type) + 1;
				affectedDbs.Add(dbIndex);
				totalSeeded++;
			}
			catch (Exception ex)
			{
				failedKeys++;
				firstFailure ??= ex.Message;
				logger.LogWarning(
					ex,
					"Sample data seeding stopped after {Count} confirmed keys (key {Key}, type {Type}, DB {Db})",
					totalSeeded,
					keyName,
					type,
					dbIndex);
				break;
			}

			if (onProgress is not null)
			{
				await onProgress(totalSeeded, totalKeys);
			}
		}

		string? errorSummary = failedKeys > 0
			? "Seeding stopped while writing a key. Some changes may be unconfirmed."
			: null;

		var finalDbs = affectedDbs.Count > 0 ? affectedDbs.OrderBy(x => x).ToList() : targetDbs;

		var result = new SeedDataResult
		{
			RequestedKeys = totalKeys,
			ProcessedKeys = processedKeys,
			TotalKeysSeeded = totalSeeded,
			FailedKeys = failedKeys,
			CountByType = countsByType,
			DatabasesAffected = finalDbs,
			Cancelled = cancelled,
			MayHaveUnconfirmedChanges = failedKeys > 0,
			Error = errorSummary,
			Message = failedKeys > 0
				? $"Requested {totalKeys}, processed {processedKeys}, persisted {totalSeeded}, failed {failedKeys} keys across DB {string.Join(", DB ", finalDbs)}"
				: $"Seeded {totalSeeded} keys across DB {string.Join(", DB ", finalDbs)}"
		};

		logger.LogInformation(
			"Seeded demo data: requested={Requested}, processed={Processed}, persisted={Persisted}, failed={Failed} across DBs: {Dbs}",
			totalKeys,
			processedKeys,
			totalSeeded,
			failedKeys,
			string.Join(", ", finalDbs));
		return Result.Ok(result);
	}

	private static List<int> ResolveTargetDatabases(SeedSampleDataOptions options, bool isCluster)
	{
		if (isCluster)
		{
			if (options.Databases != null && options.Databases.Count > 0)
			{
				return options.Databases.Distinct().ToList();
			}
			return [0];
		}

		if (options.RandomizeDatabases)
		{
			int count = Math.Clamp(options.RandomDatabasesCount, 1, 16);
			var rnd = new Random();
			return Enumerable.Range(0, 16).OrderBy(_ => rnd.Next()).Take(count).OrderBy(x => x).ToList();
		}

		if (options.Databases != null && options.Databases.Count > 0)
		{
			var list = options.Databases.Distinct().Where(db => db >= 0 && db < 16).ToList();
			if (list.Count > 0) return list;
		}

		return [0, 1, 2];
	}
}

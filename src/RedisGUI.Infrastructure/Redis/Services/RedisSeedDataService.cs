using RedisGUI.Domain.Redis;
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
	ILogger<RedisSeedDataService> logger) : IRedisSeedDataService
{
	/// <inheritdoc />
	public async Task<Result<SeedDataResult>> SeedSampleDataAsync(
		RedisConnection connection,
		SeedSampleDataOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<SeedDataResult>(multiplexerResult.Error);
		}

		var multiplexer = multiplexerResult.Value;
		var optionsActual = options ?? new SeedSampleDataOptions();
		int totalKeys = Math.Clamp(optionsActual.TotalKeys, 1, 10000);
		string prefix = string.IsNullOrWhiteSpace(optionsActual.CustomPrefix) ? "demo" : optionsActual.CustomPrefix.Trim().TrimEnd(':');

		// Resolve databases
		List<int> targetDbs = ResolveTargetDatabases(optionsActual);

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
		int totalSeeded = 0;

		var namingStrategy = KeyNamingStrategyFactory.GetStrategy(optionsActual.KeyComplexity);

		for (int i = 0; i < totalKeys; i++)
		{
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
			await seedStrategy.SeedAsync(db, keyName, optionsActual.PayloadComplexity, i, ttl, rng);

			countsByType[type] = countsByType.GetValueOrDefault(type) + 1;
			totalSeeded++;
		}

		var result = new SeedDataResult
		{
			TotalKeysSeeded = totalSeeded,
			CountByType = countsByType,
			DatabasesAffected = targetDbs,
			Message = $"Successfully seeded {totalSeeded} keys across DB {string.Join(", DB ", targetDbs)}"
		};

		logger.LogInformation("Successfully seeded demo data: {Count} keys across DBs: {Dbs}", totalSeeded, string.Join(", ", targetDbs));
		return Result.Success(result);
	}

	private static List<int> ResolveTargetDatabases(SeedSampleDataOptions options)
	{
		if (options.RandomizeDatabases)
		{
			int count = Math.Clamp(options.RandomDatabasesCount, 1, 8);
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

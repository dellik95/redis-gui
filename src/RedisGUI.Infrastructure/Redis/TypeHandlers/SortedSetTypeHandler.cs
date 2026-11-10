using RedisGUI.Domain.Errors;
using StackExchange.Redis;
using System.Linq;
using System.Text.Json;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Type handler strategy for Redis Sorted Set data types.
/// </summary>
public sealed class SortedSetTypeHandler : IRedisTypeHandler
{
	public RedisType Type => RedisType.SortedSet;

	public async Task<Result<object?>> ReadRawAsync(IDatabase database, string key)
	{
		SortedSetEntry[] entries = await database.SortedSetRangeByRankWithScoresAsync(key);
		return Result.Success<object?>(entries);
	}

	public string Serialize(object? rawValue)
	{
		var zsetEntries = rawValue is SortedSetEntry[] za
			? za.ToDictionary(x => (string?)x.Element ?? string.Empty, x => x.Score)
			: (rawValue as IDictionary<string, double>) ?? new Dictionary<string, double>();

		return JsonSerializer.Serialize(new { membersWithScores = zsetEntries });
	}

	public async Task<Result> WriteAsync(IDatabase database, string key, object value, TimeSpan? ttl)
	{
		SortedSetEntry[] sseEntries;
		if (value is IEnumerable<SortedSetEntry> sse)
		{
			sseEntries = sse.ToArray();
		}
		else if (value is IDictionary<string, double> dict)
		{
			sseEntries = dict.Select(kv => new SortedSetEntry(kv.Key, kv.Value)).ToArray();
		}
		else
		{
			return Result.Failure(DomainErrors.Redis.InvalidValueType);
		}

		await database.KeyDeleteAsync(key);
		if (sseEntries.Length > 0)
		{
			await database.SortedSetAddAsync(key, sseEntries);
		}

		if (ttl.HasValue && ttl.Value.TotalSeconds > 0)
		{
			await database.KeyExpireAsync(key, ttl.Value);
		}
		else if (ttl.HasValue && ttl.Value.TotalSeconds <= 0)
		{
			await database.KeyPersistAsync(key);
		}

		return Result.Success();
	}
}

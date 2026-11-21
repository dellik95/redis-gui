using FluentResults;
using StackExchange.Redis;
using System.Linq;
using System.Text.Json;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Type handler strategy for Redis Stream data types.
/// </summary>
public sealed class StreamTypeHandler : IRedisTypeHandler
{
	public RedisType Type => RedisType.Stream;

	public Task<Result<object?>> ReadRawAsync(IDatabase database, string key) =>
		ReadRawAsync(database, (RedisKey)key);

	public async Task<Result<object?>> ReadRawAsync(IDatabase database, RedisKey key)
	{
		StreamEntry[] entries = await database.StreamRangeAsync(key, count: 100);
		var formatted = entries.Select(entry => new
		{
			Id = entry.Id.ToString(),
			Values = entry.Values.ToDictionary(nv => nv.Name.ToString(), nv => nv.Value.ToString())
		}).ToArray();

		return Result.Ok<object?>(formatted);
	}

	public string Serialize(object? rawValue)
	{
		return JsonSerializer.Serialize(new { entries = rawValue });
	}

	public Task<Result> WriteAsync(IDatabase database, string key, object value, TimeSpan? ttl) =>
		WriteAsync(database, (RedisKey)key, value, ttl);

	public async Task<Result> WriteAsync(IDatabase database, RedisKey key, object value, TimeSpan? ttl)
	{
		if (value is IEnumerable<Dictionary<string, string>> streamEntries)
		{
			await database.KeyDeleteAsync(key);
			foreach (var entry in streamEntries)
			{
				var nvs = entry.Select(kvp => new NameValueEntry(kvp.Key, kvp.Value)).ToArray();
				if (nvs.Length > 0)
				{
					await database.StreamAddAsync(key, nvs);
				}
			}
		}

		if (ttl.HasValue && ttl.Value.TotalSeconds > 0)
		{
			await database.KeyExpireAsync(key, ttl.Value);
		}
		else if (ttl.HasValue && ttl.Value.TotalSeconds <= 0)
		{
			await database.KeyPersistAsync(key);
		}

		return Result.Ok();
	}
}

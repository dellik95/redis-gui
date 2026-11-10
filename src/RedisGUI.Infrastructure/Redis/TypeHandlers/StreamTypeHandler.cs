using RedisGUI.Shared.Primitives;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Type handler strategy for Redis Stream data types.
/// </summary>
public sealed class StreamTypeHandler : IRedisTypeHandler
{
	public RedisType Type => RedisType.Stream;

	public async Task<Result<object?>> ReadRawAsync(IDatabase database, string key)
	{
		StreamEntry[] entries = await database.StreamRangeAsync(key, count: 100);
		var formatted = entries.Select(entry => new
		{
			Id = entry.Id.ToString(),
			Values = entry.Values.ToDictionary(nv => nv.Name.ToString(), nv => nv.Value.ToString())
		}).ToArray();

		return Result.Success<object?>(formatted);
	}

	public string Serialize(object? rawValue)
	{
		return JsonSerializer.Serialize(new { entries = rawValue });
	}

	public async Task<Result> WriteAsync(IDatabase database, string key, object value, TimeSpan? ttl)
	{
		if (value is IEnumerable<Dictionary<string, string>> streamEntries)
		{
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

		return Result.Success();
	}
}

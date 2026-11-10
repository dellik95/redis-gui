using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Type handler strategy for Redis Hash data types.
/// </summary>
public sealed class HashTypeHandler : IRedisTypeHandler
{
	public RedisType Type => RedisType.Hash;

	public async Task<Result<object?>> ReadRawAsync(IDatabase database, string key)
	{
		HashEntry[] entries = await database.HashGetAllAsync(key);
		return Result.Success<object?>(entries);
	}

	public string Serialize(object? rawValue)
	{
		var hashEntries = rawValue is HashEntry[] ha
			? ha.ToDictionary(x => (string?)x.Name ?? string.Empty, x => (string?)x.Value ?? string.Empty)
			: (rawValue as IDictionary<string, string>) ?? new Dictionary<string, string>();

		return JsonSerializer.Serialize(new { fields = hashEntries });
	}

	public async Task<Result> WriteAsync(IDatabase database, string key, object value, TimeSpan? ttl)
	{
		if (value is not IDictionary<string, string> hashValues)
		{
			return Result.Failure(DomainErrors.Redis.InvalidValueType);
		}

		await database.KeyDeleteAsync(key);
		HashEntry[] entries = hashValues.Select(kvp => new HashEntry(kvp.Key, kvp.Value)).ToArray();
		if (entries.Length > 0)
		{
			await database.HashSetAsync(key, entries);
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

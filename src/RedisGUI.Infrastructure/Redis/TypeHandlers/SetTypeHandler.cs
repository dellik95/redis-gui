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
/// Type handler strategy for Redis Set data types.
/// </summary>
public sealed class SetTypeHandler : IRedisTypeHandler
{
	public RedisType Type => RedisType.Set;

	public async Task<Result<object?>> ReadRawAsync(IDatabase database, string key)
	{
		StackExchange.Redis.RedisValue[] members = await database.SetMembersAsync(key);
		return Result.Success<object?>(members);
	}

	public string Serialize(object? rawValue)
	{
		var setMembers = rawValue is StackExchange.Redis.RedisValue[] sa
			? sa.Select(x => (string?)x ?? string.Empty).ToList()
			: (rawValue as IEnumerable<string>)?.ToList() ?? new List<string>();

		return JsonSerializer.Serialize(new { members = setMembers });
	}

	public async Task<Result> WriteAsync(IDatabase database, string key, object value, TimeSpan? ttl)
	{
		if (value is not IEnumerable<string> setValues)
		{
			return Result.Failure(DomainErrors.Redis.InvalidValueType);
		}

		await database.KeyDeleteAsync(key);
		var setArr = setValues.Select(v => (StackExchange.Redis.RedisValue)v).ToArray();
		if (setArr.Length > 0)
		{
			await database.SetAddAsync(key, setArr);
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

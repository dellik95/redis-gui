using RedisGUI.Domain.Errors;
using StackExchange.Redis;
using System.Linq;
using System.Text.Json;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Type handler strategy for Redis List data types.
/// </summary>
public sealed class ListTypeHandler : IRedisTypeHandler
{
	public RedisType Type => RedisType.List;

	public async Task<Result<object?>> ReadRawAsync(IDatabase database, string key)
	{
		StackExchange.Redis.RedisValue[] items = await database.ListRangeAsync(key);
		return Result.Success<object?>(items);
	}

	public string Serialize(object? rawValue)
	{
		var listItems = rawValue is StackExchange.Redis.RedisValue[] la
			? la.Select(x => (string?)x ?? string.Empty).ToList()
			: (rawValue as IEnumerable<string>)?.ToList() ?? new List<string>();

		return JsonSerializer.Serialize(new { items = listItems });
	}

	public async Task<Result> WriteAsync(IDatabase database, string key, object value, TimeSpan? ttl)
	{
		if (value is not IEnumerable<string> listValues)
		{
			return Result.Failure(DomainErrors.Redis.InvalidValueType);
		}

		await database.KeyDeleteAsync(key);
		var listArr = listValues.Select(v => (StackExchange.Redis.RedisValue)v).ToArray();
		if (listArr.Length > 0)
		{
			await database.ListRightPushAsync(key, listArr);
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

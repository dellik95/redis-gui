using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Type handler strategy for Redis String data types.
/// </summary>
public sealed class StringTypeHandler : IRedisTypeHandler
{
	public RedisType Type => RedisType.String;

	public async Task<Result<object?>> ReadRawAsync(IDatabase database, string key)
	{
		StackExchange.Redis.RedisValue val = await database.StringGetAsync(key);
		return Result.Success<object?>(val);
	}

	public string Serialize(object? rawValue)
	{
		return rawValue?.ToString() ?? string.Empty;
	}

	public async Task<Result> WriteAsync(IDatabase database, string key, object value, TimeSpan? ttl)
	{
		string stringValue = value?.ToString() ?? string.Empty;
		bool ok = ttl.HasValue && ttl.Value.TotalSeconds > 0
			? await database.StringSetAsync(key, stringValue, (Expiration)ttl.Value)
			: await database.StringSetAsync(key, stringValue);

		if (ok && ttl.HasValue && ttl.Value.TotalSeconds <= 0)
		{
			await database.KeyPersistAsync(key);
		}

		return ok ? Result.Success() : Result.Failure(Domain.Errors.DomainErrors.Redis.UpdateFailed);
	}
}

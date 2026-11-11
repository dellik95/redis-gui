using RedisGUI.Domain.Redis;
using RedisValue = RedisGUI.Domain.Redis.RedisValue;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Aggregate facade service for Redis connection and key operations
/// </summary>
public class ConnectionService(
	IRedisConnectionService connectionService,
	IRedisKeyService keyService,
	IRedisValueService valueService) : IConnectionService
{
	public async Task<Result> CheckConnection(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		return await connectionService.CheckConnectionAsync(connection, cancellationToken);
	}

	public async ValueTask<Result<IEnumerable<string>>> GetKeys(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		return await keyService.GetKeysAsync(connection, cancellationToken);
	}

	public async Task<Result<long>> DeleteKeys(RedisConnection connection, string[] keys, CancellationToken cancellationToken = default)
	{
		return await keyService.DeleteKeysAsync(connection, keys, cancellationToken);
	}

	public async Task<Result> SetValue(RedisConnection connection, string key, string value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
	{
		return await valueService.SetValueAsync(connection, key, value, ttl, cancellationToken);
	}

	public async Task<Result<RedisValue>> GetValue(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		return await valueService.GetValueAsync(connection, key, cancellationToken);
	}

	public async Task<Result<PagedRedisValues>> GetAllValues(
		RedisConnection connection,
		string pattern = "*",
		int? pageSize = null,
		int? pageNumber = null,
		CancellationToken cancellationToken = default)
	{
		return await valueService.GetAllValuesAsync(connection, pattern, pageSize, pageNumber, cancellationToken);
	}

	public async Task<Result<bool>> DeleteKey(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		return await keyService.DeleteKeyAsync(connection, key, cancellationToken);
	}

	public async Task<Result<(int DeletedCount, IReadOnlyList<string> FailedKeys)>> DeleteMultipleKeys(
		RedisConnection connection,
		IReadOnlyList<string> keys,
		CancellationToken cancellationToken = default)
	{
		return await keyService.DeleteMultipleKeysAsync(connection, keys, cancellationToken);
	}

	public async Task<Result<int>> DeleteKeysByPattern(RedisConnection connection, string pattern, CancellationToken cancellationToken = default)
	{
		return await keyService.DeleteKeysByPatternAsync(connection, pattern, cancellationToken);
	}

	public async Task<Result<string>> GetKeyType(RedisConnection connection, string key, CancellationToken cancellationToken = default)
	{
		return await keyService.GetKeyTypeAsync(connection, key, cancellationToken);
	}

	public async Task<Result> UpdateValue(RedisConnection connection, string key, object value, string type, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
	{
		return await valueService.UpdateValueAsync(connection, key, value, type, ttl, cancellationToken);
	}
}

using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Connection;

public interface IConnectionService
{
	Task<Result> TestConnection(RedisConnection connection);

	ValueTask<IEnumerable<string>> GetKeys(RedisConnection connection);

	Task<long> DeleteKeys(RedisConnection connection, string[] strings);
}
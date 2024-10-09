using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

public class ConnectionService: IConnectionService
{
	private readonly IConnectionPool _connectionPool;

	public ConnectionService(IConnectionPool connectionPool)
	{
		_connectionPool = connectionPool;
	}

	public async Task<Result> TestConnection(RedisConnection connection)
	{
		var redisConfig = MapConfigurationOptions(connection);

		var multiplexer = await this._connectionPool.GetConnection(redisConfig);

		var result = multiplexer.GetDatabase(connection.Database).Ping();
		return (result.CompareTo(TimeSpan.Zero) > 0) ? Result.Success() : Result.Failure(DomainErrors.Connection.ConnectionNotEstablished);
	}

	public async ValueTask<IEnumerable<string>> GetKeys(RedisConnection connection)
	{
		var redisConfig = MapConfigurationOptions(connection);

		var multiplexer = await this._connectionPool.GetConnection(redisConfig);
		var servers = multiplexer.GetServers().Where(s => s is { IsConnected: true, Features.Scan: true });
		var keys = new List<string>();

		return servers.Select(server => server.Keys(connection.Database).Select(x => x.ToString())).Aggregate(keys, (current, serverKeys) => current.Union(serverKeys).ToList());
	}

	public async Task<long> DeleteKeys(RedisConnection connection, string[] keys)
	{
		var redisConfig = MapConfigurationOptions(connection);

		var multiplexer = await this._connectionPool.GetConnection(redisConfig);
		var database = multiplexer.GetDatabase(connection.Database);

		var redisKeys = keys.Select(x => new RedisKey(x)).ToArray();
		var deletedCount = await database.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
		return deletedCount;
	}

	private static ConfigurationOptions MapConfigurationOptions(RedisConnection connection)
	{
		//TODO: Add Login And Password

		var redisConfig = new ConfigurationOptions()
		{
			EndPoints = new EndPointCollection()
			{
				$"{connection.Host}:{connection.Port}"
			},
			AbortOnConnectFail = true,
			AllowAdmin = true,
			ConnectTimeout = 10
		};
		return redisConfig;
	}
}
using System.Collections.Concurrent;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

internal sealed class ConnectionPool : IConnectionPool
{
	private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connectionPool = new();

	public async Task<ConnectionMultiplexer> GetConnection(ConfigurationOptions connectionData)
	{
		ArgumentNullException.ThrowIfNull(connectionData, nameof(connectionData));
		var key = connectionData.ToString();
		if (this._connectionPool.TryGetValue(key, out var multiplexer))
		{
			return multiplexer;
		}

		multiplexer = await ConnectionMultiplexer.ConnectAsync(connectionData);
		this._connectionPool.TryAdd(key, multiplexer);

		return multiplexer;
	}

	public void Dispose()
	{
		foreach (var multiplexer in _connectionPool.Values)
		{
			multiplexer.Dispose();
		}

		this._connectionPool.Clear();
	}

	public async ValueTask DisposeAsync()
	{
		var tasks = _connectionPool.Values.Select(x => x.DisposeAsync().AsTask());
		await Task.WhenAll(tasks);
		this._connectionPool.Clear();
	}

}
using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Redis messaging operations using the shared connection manager.</summary>
public sealed class RedisMessagingService(
	IRedisConnectionManager connections,
	IRedisTopologyRegistry? topology = null)
{
	public async Task<ConnectionMultiplexer> ConnectAsync(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		var result = await connections.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (result.IsFailure) throw new InvalidOperationException(result.Error.Message);
		topology?.SetMode(connection.Id, result.Value.DetectServerMode());
		return result.Value;
	}

	public IDisposable? Pin(ConnectionMultiplexer multiplexer) => connections.PinConnection(multiplexer);

	public async Task<long> PublishAsync(RedisConnection connection, string channel, string message, CancellationToken cancellationToken)
	{
		var redis = await ConnectAsync(connection, cancellationToken);
		return await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), message).WaitAsync(cancellationToken);
	}

	public async Task<object> StreamAsync(RedisConnection connection, string key, CancellationToken cancellationToken)
	{
		var redis = await ConnectAsync(connection, cancellationToken);
		int targetDb = redis.IsCluster() ? 0 : connection.DatabaseNumber.Value;
		var db = redis.GetDatabase(targetDb);
		var info = await db.StreamInfoAsync(key).WaitAsync(cancellationToken);
		var entries = await db.StreamRangeAsync(key, maxId: info.LastGeneratedId, count: 100, messageOrder: Order.Descending).WaitAsync(cancellationToken);
		return new
		{
			length = info.Length,
			groups = (await db.StreamGroupInfoAsync(key).WaitAsync(cancellationToken)).Length,
			lastGeneratedId = info.LastGeneratedId.ToString(),
			entries = entries.Select(ToEntry)
		};
	}

	public static object ToEntry(StreamEntry entry) => new
	{
		id = entry.Id.ToString(),
		fields = entry.Values.Select(v => new { name = v.Name.ToString(), value = v.Value.ToString() })
	};
}

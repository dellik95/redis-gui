using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Collects point-in-time messaging counts from the selected Redis server and database.</summary>
public sealed class RedisMessagingMetricsCollector(RedisMessagingService messaging)
{
	public async Task<object> CollectAsync(RedisConnection connection, string channel, CancellationToken cancellationToken)
	{
		var redis = await messaging.ConnectAsync(connection, cancellationToken);
		var server = redis.GetConnectedServer(connection);
		var subscribers = await server.SubscriptionSubscriberCountAsync(RedisChannel.Literal(channel));
		var patterns = await server.SubscriptionPatternCountAsync();
		return new { subscribers, patterns };
	}

	public static async Task<(long? Subscribers, long Patterns)> CollectChannelAsync(ConnectionMultiplexer redis, RedisConnection connection, string channel, bool pattern)
	{
		var server = redis.GetConnectedServer(connection);
		long? subscribers = pattern ? null : await server.SubscriptionSubscriberCountAsync(RedisChannel.Literal(channel));
		return (subscribers, await server.SubscriptionPatternCountAsync());
	}

	public static async Task<(long Length, long Groups)> CollectStreamAsync(IDatabase database, string key)
	{
		var info = await database.StreamInfoAsync(key);
		return (info.Length, (await database.StreamGroupInfoAsync(key)).Length);
	}
}

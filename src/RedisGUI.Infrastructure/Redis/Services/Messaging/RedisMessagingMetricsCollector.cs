using System.Linq;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Collects point-in-time messaging counts across standalone or Cluster Redis nodes.</summary>
public sealed class RedisMessagingMetricsCollector(RedisMessagingService messaging)
{
	public async Task<object> CollectAsync(RedisConnection connection, string channel, CancellationToken cancellationToken)
	{
		var redis = await messaging.ConnectAsync(connection, cancellationToken);
		var (subscribers, patterns) = await CollectChannelInternalAsync(redis, connection, channel, pattern: false);
		return new { subscribers = subscribers ?? 0, patterns };
	}

	public static async Task<(long? Subscribers, long Patterns)> CollectChannelAsync(
		ConnectionMultiplexer redis,
		RedisConnection connection,
		string channel,
		bool pattern)
	{
		var (subscribers, patterns) = await CollectChannelInternalAsync(redis, connection, channel, pattern);
		return (subscribers, patterns);
	}

	private static async Task<(long? Subscribers, long Patterns)> CollectChannelInternalAsync(
		ConnectionMultiplexer redis,
		RedisConnection connection,
		string channel,
		bool pattern)
	{
		if (redis.IsCluster())
		{
			var servers = redis.GetServers().Where(s => s.IsConnected && s.ServerType == ServerType.Cluster).ToArray();
			if (servers.Length > 0)
			{
				long? totalSubscribers = pattern ? null : 0;
				long totalPatterns = 0;
				foreach (var node in servers)
				{
					if (!pattern)
					{
						totalSubscribers += await node.SubscriptionSubscriberCountAsync(RedisChannel.Literal(channel));
					}
					totalPatterns += await node.SubscriptionPatternCountAsync();
				}
				return (totalSubscribers, totalPatterns);
			}
		}

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

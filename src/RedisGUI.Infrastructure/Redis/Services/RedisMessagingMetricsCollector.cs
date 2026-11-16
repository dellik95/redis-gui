using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Collects point-in-time pub/sub counts from the connected Redis server.</summary>
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
}

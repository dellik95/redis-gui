using StackExchange.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>Redis messaging operations using the shared connection manager.</summary>
public sealed class RedisMessagingService(IRedisConnectionManager connections)
{
    public async Task<ConnectionMultiplexer> ConnectAsync(RedisConnection connection, CancellationToken cancellationToken = default)
    {
        var result = await connections.GetConnection(connection.BuildConnectionString(), cancellationToken);
        if (result.IsFailure) throw new InvalidOperationException(result.Error.Message);
        return result.Value;
    }

    public IDisposable? Pin(ConnectionMultiplexer multiplexer) => connections.PinConnection(multiplexer);

    public async Task<long> PublishAsync(RedisConnection connection, string channel, string message, CancellationToken cancellationToken)
    {
        var redis = await ConnectAsync(connection, cancellationToken);
        return await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), message);
    }

    public async Task<object> StreamAsync(RedisConnection connection, string key, CancellationToken cancellationToken)
    {
        var redis = await ConnectAsync(connection, cancellationToken);
        var db = redis.GetDatabase(connection.DatabaseNumber.Value);
        var info = await db.StreamInfoAsync(key);
        var entries = await db.StreamRangeAsync(key, count: 100, messageOrder: Order.Descending);
        return new { length = info.Length, groups = (await db.StreamGroupInfoAsync(key)).Length, lastGeneratedId = info.LastGeneratedId.ToString(), entries = entries.Select(ToEntry) };
    }

    public static object ToEntry(StreamEntry entry) => new { id = entry.Id.ToString(), fields = entry.Values.Select(v => new { name = v.Name.ToString(), value = v.Value.ToString() }) };
}

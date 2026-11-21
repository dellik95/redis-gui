using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.Redis;
using RedisGUI.Infrastructure.Redis.Services;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace RedisGUI.Infrastructure.SignalR.Hubs;

/// <summary>Authenticated, per-client messaging subscriptions.</summary>
[Authorize]
public sealed class MessagingHub(IRedisConnectionRepository repository, IAuthorizationService authorization,
    RedisMessagingService messaging, MessagingSubscriptions subscriptions) : Hub
{
    private async Task<RedisConnection> GetConnection(Guid id)
    {
        var result = await repository.GetConnectionByIdAsync(id, Context.ConnectionAborted);
        if (result.IsFailed || !(await authorization.AuthorizeAsync(Context.User!, result.Value, new ConnectionOwnershipRequirement())).Succeeded)
            throw new HubException("Connection not found or access denied.");
        return result.Value;
    }

    public async Task WatchChannel(Guid connectionId, string channel, bool pattern)
    {
        if (string.IsNullOrWhiteSpace(channel) || channel.Length > 512) throw new HubException("Enter a channel or pattern (up to 512 characters).");
        var connection = await GetConnection(connectionId);
        var redis = await messaging.ConnectAsync(connection, Context.ConnectionAborted);
        var lease = messaging.Pin(redis) ?? throw new HubException("Connection was retired. Please retry.");
        await subscriptions.WatchChannelAsync(Context.ConnectionId, redis, connection, channel, pattern, Clients.Caller, lease);
    }

    public async Task WatchStream(Guid connectionId, string key, string afterId)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 512 || string.IsNullOrWhiteSpace(afterId)) throw new HubException("Enter a stream key and starting ID.");
        var connection = await GetConnection(connectionId);
        var redis = await messaging.ConnectAsync(connection, Context.ConnectionAborted);
        var lease = messaging.Pin(redis) ?? throw new HubException("Connection was retired. Please retry.");
        await subscriptions.WatchStreamAsync(Context.ConnectionId, redis.GetDatabase(redis.IsCluster() ? 0 : connection.DatabaseNumber.Value), key, afterId, Clients.Caller, lease);
    }

    public Task StopWatching() => subscriptions.RemoveAsync(Context.ConnectionId);
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await subscriptions.RemoveAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>Owns the lifetime of one active subscription per SignalR client.</summary>
public sealed class MessagingSubscriptions(ILogger<MessagingSubscriptions> logger)
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> watches = new();
    public async Task RemoveAsync(string clientId)
    {
        if (watches.TryRemove(clientId, out var cts))
        {
            try { await cts.CancelAsync(); } catch (ObjectDisposedException) { }
        }
    }
    public async Task WatchChannelAsync(string clientId, ConnectionMultiplexer redis, RedisConnection connection, string channel, bool pattern, IClientProxy client, IDisposable lease)
    {
        await RemoveAsync(clientId);
        var cts = new CancellationTokenSource();
        watches[clientId] = cts;
        try
        {
            var queue = await redis.GetSubscriber().SubscribeAsync(pattern ? RedisChannel.Pattern(channel) : RedisChannel.Literal(channel));
            var counter = new DeliveryCounter();
            _ = RunChannelAsync(clientId, cts, queue, client, lease, counter, () => RunMetricsAsync(cts.Token, client, counter, async () =>
            {
                var (subscribers, patterns) = await RedisMessagingMetricsCollector.CollectChannelAsync(redis, connection, channel, pattern);
                return (subscribers, (long?)patterns);
            }));
        }
        catch { watches.TryRemove(clientId, out _); cts.Dispose(); lease.Dispose(); throw; }
    }
    private async Task RunChannelAsync(string clientId, CancellationTokenSource cts, ChannelMessageQueue queue, IClientProxy client, IDisposable lease, DeliveryCounter counter, Func<Task> collect)
    {
        var metricsTask = collect();
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var message = await queue.ReadAsync(cts.Token);
                await client.SendAsync("Message", new { channel = message.Channel.ToString(), payload = message.Message.ToString(), receivedAt = DateTimeOffset.UtcNow }, cts.Token);
                Interlocked.Increment(ref counter.Value);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { logger.LogWarning(ex, "Messaging subscription failed for {ClientId}", clientId); try { await client.SendAsync("MonitorError", "Channel monitoring stopped. Check the connection."); } catch (Exception) { } }
        finally
        {
            try { await cts.CancelAsync(); try { await queue.UnsubscribeAsync(); } finally { await metricsTask; } }
            finally { watches.TryRemove(new KeyValuePair<string, CancellationTokenSource>(clientId, cts)); cts.Dispose(); lease.Dispose(); }
        }
    }
    public async Task WatchStreamAsync(string clientId, IDatabase db, string key, string afterId, IClientProxy client, IDisposable lease)
    {
        await RemoveAsync(clientId);
        var cts = new CancellationTokenSource();
        watches[clientId] = cts;
        var counter = new DeliveryCounter();
        _ = RunStreamAsync(clientId, cts, db, key, afterId, client, lease, counter, () => RunMetricsAsync(cts.Token, client, counter, async () =>
        {
            var (length, groups) = await RedisMessagingMetricsCollector.CollectStreamAsync(db, key);
            return ((long?)length, (long?)groups);
        }));
    }
    private async Task RunStreamAsync(string clientId, CancellationTokenSource cts, IDatabase db, string key, string lastId, IClientProxy client, IDisposable lease, DeliveryCounter counter, Func<Task> collect)
    {
        var metricsTask = collect();
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var entries = await db.StreamReadAsync(key, lastId, count: 100);
                foreach (var entry in entries) { await client.SendAsync("StreamEntry", RedisMessagingService.ToEntry(entry), cts.Token); lastId = entry.Id.ToString(); Interlocked.Increment(ref counter.Value); }
                if (entries.Length < 100) await Task.Delay(1000, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { logger.LogWarning(ex, "Stream monitor failed for {ClientId}", clientId); try { await client.SendAsync("MonitorError", "Stream monitoring stopped. Check the key and connection."); } catch (Exception) { } }
        finally
        {
            try { await cts.CancelAsync(); await metricsTask; }
            finally { watches.TryRemove(new KeyValuePair<string, CancellationTokenSource>(clientId, cts)); cts.Dispose(); lease.Dispose(); }
        }
    }

    private sealed class DeliveryCounter { public long Value; }

    private async Task RunMetricsAsync(CancellationToken cancellationToken, IClientProxy client, DeliveryCounter counter, Func<Task<(long? Primary, long? Secondary)>> collect)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        long previous = 0;
        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                try
                {
                    var (primary, secondary) = await collect();
                    var elapsed = clock.Elapsed.TotalSeconds;
                    var delivered = Interlocked.Read(ref counter.Value);
                    await client.SendAsync("MessagingMetrics", new { timestamp = DateTimeOffset.UtcNow, primary, secondary, delivered, rate = (delivered - previous) / elapsed }, cancellationToken);
                    previous = delivered;
                    clock.Restart();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not collect messaging metrics");
                    try { await client.SendAsync("MetricsError", "Metrics unavailable. Retrying…", cancellationToken); } catch (Exception) { }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
}

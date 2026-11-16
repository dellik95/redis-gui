using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Infrastructure.SignalR.Hubs;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public class MessagingMetricsIntegrationTests
{
    [Test]
    public async Task Watches_StreamLiveCountsAndDeliveryRates()
    {
        var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
        if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
        using var redis = await ConnectionMultiplexer.ConnectAsync(configuration!);
        var subscriptions = new MessagingSubscriptions(NullLogger<MessagingSubscriptions>.Instance);
        var samples = new ConcurrentQueue<JsonElement>();
        var client = new Mock<IClientProxy>();
        client.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((method, args, _) =>
            {
                if (method == "MessagingMetrics") samples.Enqueue(JsonSerializer.SerializeToElement(args[0]));
            }).Returns(Task.CompletedTask);
        var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Metrics test"), new ConnectionHost("127.0.0.1"), new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
        var key = "redisgui:metrics:" + Guid.NewGuid();
        var db = redis.GetDatabase();
        try
        {
            await subscriptions.WatchChannelAsync("channel", redis, connection, key, false, client.Object, new Lease());
            await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(key), "one");
            var channelSample = await WaitForAsync(samples, sample => sample.GetProperty("delivered").GetInt64() > 0);
            Assert.That(channelSample.GetProperty("primary").GetInt64(), Is.GreaterThanOrEqualTo(1));
            Assert.That(channelSample.GetProperty("rate").GetDouble(), Is.GreaterThan(0));
            await subscriptions.RemoveAsync("channel");
            while (samples.TryDequeue(out _)) { }

            await subscriptions.WatchChannelAsync("pattern", redis, connection, key + "*", true, client.Object, new Lease());
            await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(key), "two");
            var patternSample = await WaitForAsync(samples, sample => sample.GetProperty("primary").ValueKind == JsonValueKind.Null);
            Assert.That(patternSample.GetProperty("secondary").GetInt64(), Is.GreaterThanOrEqualTo(1));
            await subscriptions.RemoveAsync("pattern");
            while (samples.TryDequeue(out _)) { }

            await db.StreamAddAsync(key, "event", "initial");
            await db.StreamCreateConsumerGroupAsync(key, "workers", "0-0");
            await subscriptions.WatchStreamAsync("stream", db, key, "0-0", client.Object, new Lease());
            var streamSample = await WaitForAsync(samples, sample => sample.GetProperty("primary").ValueKind == JsonValueKind.Number && sample.GetProperty("secondary").GetInt64() == 1 && sample.GetProperty("delivered").GetInt64() > 0);
            Assert.That(streamSample.GetProperty("primary").GetInt64(), Is.EqualTo(1));
            Assert.That(streamSample.GetProperty("secondary").GetInt64(), Is.EqualTo(1));
        }
        finally
        {
            await subscriptions.RemoveAsync("channel");
            await subscriptions.RemoveAsync("pattern");
            await subscriptions.RemoveAsync("stream");
            await db.KeyDeleteAsync(key);
        }
    }

    private static async Task<JsonElement> WaitForAsync(ConcurrentQueue<JsonElement> samples, Func<JsonElement, bool> matches)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!timeout.IsCancellationRequested)
        {
            while (samples.TryDequeue(out var sample)) if (matches(sample)) return sample;
            await Task.Delay(50);
        }
        throw new TimeoutException("No matching live metrics sample arrived.");
    }

    private sealed class Lease : IDisposable { public void Dispose() { } }
}

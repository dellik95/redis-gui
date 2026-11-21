using FluentResults;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public class RedisMessagingServiceTests
{
    [Test]
    public async Task StreamSnapshot_DoesNotIncludeEntriesBeyondItsLiveCursor()
    {
        var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
        if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
        using var redis = await ConnectionMultiplexer.ConnectAsync(configuration!);
        var manager = new Mock<IRedisConnectionManager>();
        manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(redis));
        var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Messaging test"), new ConnectionHost("redis"), new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
        var service = new RedisMessagingService(manager.Object);
        var key = "redisgui:test:" + Guid.NewGuid();
        var db = redis.GetDatabase();
        await db.StreamAddAsync(key, "event", "initial");
        using var stop = new CancellationTokenSource();
        var writer = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested) await db.StreamAddAsync(key, "event", "new");
        });
        try
        {
            for (int i = 0; i < 50; i++)
            {
                using var snapshot = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(await service.StreamAsync(connection, key, CancellationToken.None)));
                var cursor = ParseId(snapshot.RootElement.GetProperty("lastGeneratedId").GetString()!);
                foreach (var entry in snapshot.RootElement.GetProperty("entries").EnumerateArray())
                    Assert.That(ParseId(entry.GetProperty("id").GetString()!).CompareTo(cursor), Is.LessThanOrEqualTo(0));
            }
        }
        finally
        {
            stop.Cancel();
            await writer;
            await db.KeyDeleteAsync(key);
        }
    }

    private static (long, long) ParseId(string value)
    {
        var parts = value.Split('-');
        return (long.Parse(parts[0]), long.Parse(parts[1]));
    }

    [Test]
    public async Task PublishAndStreamSnapshot_UseTheConfiguredRedisConnection()
    {
        var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
        if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
        using var redis = await ConnectionMultiplexer.ConnectAsync(configuration!);
        var manager = new Mock<IRedisConnectionManager>();
        manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(redis));
        var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Messaging test"), new ConnectionHost("redis"), new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
        var service = new RedisMessagingService(manager.Object);
        var key = "redisgui:test:" + Guid.NewGuid();
        var channel = RedisChannel.Literal(key);
        var subscriber = redis.GetSubscriber();
        var queue = await subscriber.SubscribeAsync(channel);
        try
        {
            await redis.GetDatabase().StreamAddAsync(key, "event", "created");
            var snapshot = await service.StreamAsync(connection, key, CancellationToken.None);
            var json = System.Text.Json.JsonSerializer.Serialize(snapshot);
            Assert.That(json, Does.Contain("created"));
            Assert.That(json, Does.Contain("lastGeneratedId"));
            Assert.That(await service.PublishAsync(connection, key, "hello", CancellationToken.None), Is.GreaterThanOrEqualTo(1));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var message = await queue.ReadAsync(timeout.Token);
            Assert.That(message.Message.ToString(), Is.EqualTo("hello"));
        }
        finally
        {
            await queue.UnsubscribeAsync();
            await redis.GetDatabase().KeyDeleteAsync(key);
        }
    }
}

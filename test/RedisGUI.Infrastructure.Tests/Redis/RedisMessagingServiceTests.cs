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
    public async Task PublishAndStreamSnapshot_UseTheConfiguredRedisConnection()
    {
        var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
        if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
        using var redis = await ConnectionMultiplexer.ConnectAsync(configuration!);
        var manager = new Mock<IRedisConnectionManager>();
        manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(redis));
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

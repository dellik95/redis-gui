using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Shared.Primitives;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public class SeedJobIntegrationTests
{
	[Test]
	public async Task Cancellation_ReportsConfirmedKeysAndStopsBeforeNextKey()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var mux = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(x => x.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(mux));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Seed test"), new ConnectionHost("localhost"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var prefix = "redisgui-seed-test-" + Guid.NewGuid().ToString("N");
		var db = mux.GetDatabase(1);
		using var cancellation = new CancellationTokenSource();
		try
		{
			var service = new RedisSeedDataService(manager.Object, NullLogger<RedisSeedDataService>.Instance);
			var options = new SeedSampleDataOptions
			{
				TotalKeys = 10,
				Databases = [1],
				DataTypes = ["string"],
				KeyComplexity = "simple",
				PayloadComplexity = "simple",
				TtlStrategy = "persistent",
				CustomPrefix = prefix
			};
			var result = await service.SeedSampleDataAsync(connection, options, cancellation.Token,
				(_, _) => { cancellation.Cancel(); return Task.CompletedTask; });
			Assert.That(result.Value.TotalKeysSeeded, Is.EqualTo(1));
			Assert.That(result.Value.Cancelled, Is.True);
			Assert.That(await db.KeyExistsAsync(prefix + ":string:1"), Is.True);
			Assert.That(await db.KeyExistsAsync(prefix + ":string:2"), Is.False);
		}
		finally { for (int i = 1; i <= 10; i++) await db.KeyDeleteAsync(prefix + ":string:" + i); }
	}
}

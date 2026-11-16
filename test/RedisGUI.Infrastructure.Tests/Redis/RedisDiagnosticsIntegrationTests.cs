using System;
using System.Linq;
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
public class RedisDiagnosticsIntegrationTests
{
	[Test]
	public async Task SlowlogAndLoadTest_UseRealServerAndCleanUp()
	{
		if (Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS") != "127.0.0.1:16391")
			Assert.Ignore("Set REDISGUI_TEST_REDIS to the dedicated test Redis at 127.0.0.1:16391.");
		using var mux = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:16391,allowAdmin=true");
		var server = mux.GetServer("127.0.0.1", 16391);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(mux));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Test"), new ConnectionHost("127.0.0.1"), new ConnectionPort(16391), new DatabaseNumber(0), ConnectionOwnerId.None());
		await server.ConfigSetAsync("slowlog-log-slower-than", "0");
		await mux.GetDatabase().StringSetAsync("slowlog-verification", "value");
		var diagnostics = new RedisDiagnosticsService(manager.Object, NullLogger<RedisDiagnosticsService>.Instance);
		var slowlog = await diagnostics.GetSlowlogAsync(connection, 25);
		Assert.That(slowlog.Value.Any(e => e.Arguments.Any(a => a == "slowlog-verification")), Is.True);
		await mux.GetDatabase().KeyDeleteAsync("slowlog-verification");
		var service = new RedisLoadTestService(manager.Object, NullLogger<RedisLoadTestService>.Instance);
		int maxClients = 0;
		var result = await service.RunStreamingLoadTestAsync(connection, new RedisLoadTestRequest(Concurrency: 20, OperationType: "mixed", DurationSeconds: 2), async _ =>
		{
			var clients = await server.ClientListAsync();
			maxClients = Math.Max(maxClients, clients.Count(c => c.Name != null && c.Name.StartsWith("redis-gui-load-test-")));
		});
		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value.SuccessfulOperations, Is.GreaterThan(0));
		Assert.That(maxClients, Is.GreaterThanOrEqualTo(20));
		Assert.That((await server.ClientListAsync()).Count(c => c.Name != null && c.Name.StartsWith("redis-gui-load-test-")), Is.Zero);
		Assert.That(server.Keys(pattern: "__bench:*").Any(), Is.False);
	}
}

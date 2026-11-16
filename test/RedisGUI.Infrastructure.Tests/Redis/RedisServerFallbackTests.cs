using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Redis.ConnectionManager;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Infrastructure.RedisMetrics;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public class RedisServerFallbackTests
{
	[Test]
	public async Task ServerOperations_UseTheConnectedFallbackEndpoint()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration))
		{
			Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		}

		using var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(multiplexer));
		var connection = new AnonymousRedisConnection(
			Guid.NewGuid(), new ConnectionName("Fallback test"), new ConnectionHost("redis"),
			new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var metrics = new RedisMetricsCollector(manager.Object, Options.Create(new MetricsCollectorOptions()),
			NullLogger<RedisMetricsCollector>.Instance);
		var diagnostics = new RedisDiagnosticsService(manager.Object, NullLogger<RedisDiagnosticsService>.Instance);
		var keys = new RedisKeyService(manager.Object, NullLogger<RedisKeyService>.Instance);
		var key = "redisgui-fallback-test:" + Guid.NewGuid();
		try
		{
			await multiplexer.GetDatabase().StringSetAsync(key, "value");
			var collected = await metrics.CollectMetrics(connection);
			Assert.That(collected.IsSuccess, Is.True, collected.IsFailure ? collected.Error.Message : string.Empty);
			Assert.That((await diagnostics.GetServerInfoAsync(connection)).IsSuccess, Is.True);
			Assert.That((await diagnostics.GetServerConfigAsync(connection, "maxmemory")).IsSuccess, Is.True);
			Assert.That((await diagnostics.GetSlowlogAsync(connection)).IsSuccess, Is.True);
			var deletion = await keys.DeleteKeysByPatternAsync(connection, key);
			Assert.That(deletion.IsSuccess, Is.True);
			Assert.That(deletion.Value, Is.EqualTo(1));
		}
		finally
		{
			await multiplexer.GetDatabase().KeyDeleteAsync(key);
		}
	}

	[Test]
	public async Task LoadTest_UsesEffectiveEndpointWhenConnectionManagerFallsBack()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var endpoint = (System.Net.IPEndPoint)multiplexer.GetEndPoints()[0];
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(multiplexer));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Fallback test"),
			new ConnectionHost("redis"), new ConnectionPort(endpoint.Port), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisLoadTestService(manager.Object, NullLogger<RedisLoadTestService>.Instance);
		var result = await service.RunLoadTestAsync(connection, new RedisLoadTestRequest(Concurrency: 2, OperationType: "mixed", OperationsCount: 10));
		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value.SuccessfulOperations, Is.GreaterThan(0));
	}
}

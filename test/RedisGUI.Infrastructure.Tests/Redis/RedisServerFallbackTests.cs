using FluentResults;
using System;
using System.Linq;
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
	public async Task ScanKeys_RejectsContinuationAfterClusterFailover()
	{
		// This test changes cluster topology. Only run it against a disposable cluster.
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_CLUSTER_FAILOVER");
		var replicaEndpoint = Environment.GetEnvironmentVariable("REDISGUI_TEST_CLUSTER_FAILOVER_REPLICA");
		var primaryEndpoint = Environment.GetEnvironmentVariable("REDISGUI_TEST_CLUSTER_FAILOVER_PRIMARY");
		if (string.IsNullOrEmpty(configuration) || string.IsNullOrEmpty(replicaEndpoint) || string.IsNullOrEmpty(primaryEndpoint))
			Assert.Ignore("Set REDISGUI_TEST_CLUSTER_FAILOVER and the primary/replica endpoints for a disposable cluster.");
		using var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var primary = multiplexer.GetServer(primaryEndpoint!);
		var replica = multiplexer.GetServer(replicaEndpoint!);
		Assert.That(primary.IsReplica, Is.False);
		Assert.That(replica.IsReplica, Is.True);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Ok(multiplexer));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Failover test"),
			new ConnectionHost("localhost"), new ConnectionPort(17100), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisKeyService(manager.Object, NullLogger<RedisKeyService>.Instance);
		var initial = await service.ScanKeysAsync(connection, "*", 0, null, 1);
		Assert.That(initial.IsSuccess, Is.True, initial.IsFailed ? initial.GetDomainError().Message : "");
		Assert.That(initial.Value.NextCursor, Is.Not.Null);

		await replica.ExecuteAsync("CLUSTER", "FAILOVER");
		var deadline = DateTime.UtcNow.AddSeconds(30);
		string? replicaRole = null;
		string? primaryRole = null;
		while (DateTime.UtcNow < deadline)
		{
			replicaRole = (string)((RedisResult[])(await replica.ExecuteAsync("ROLE"))!)[0]!;
			primaryRole = (string)((RedisResult[])(await primary.ExecuteAsync("ROLE"))!)[0]!;
			if (replicaRole == "master" && primaryRole == "slave") break;
			await Task.Delay(200);
		}
		Assert.That(replicaRole, Is.EqualTo("master"), "Replica did not become primary.");
		Assert.That(primaryRole, Is.EqualTo("slave"), "Original primary did not become a replica.");

		var continuation = await service.ScanKeysAsync(connection, "*", 0, initial.Value.NextCursor, 1);
		Assert.That(continuation.IsFailed, Is.True, "A cursor from the old topology must not continue silently.");
		Assert.That(continuation.GetDomainError().Code, Is.AnyOf("Redis.InvalidCursor", "Redis.ScanUnavailable"));
	}

	[Test]
	public async Task ScanKeys_TraversesClusterPrimaries()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_CLUSTER");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_CLUSTER to run the Redis Cluster integration check.");
		using var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(multiplexer));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Cluster scan test"),
			new ConnectionHost("localhost"), new ConnectionPort(17000), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisKeyService(manager.Object, NullLogger<RedisKeyService>.Instance);
		var prefix = "redisgui-cluster-scan:" + Guid.NewGuid() + ":";
		var db = multiplexer.GetDatabase(0);
		try
		{
			for (var i = 0; i < 90; i++) await db.StringSetAsync(prefix + i, "value");
			string? cursor = null;
			var found = new System.Collections.Generic.HashSet<string>();
			var pages = 0;
			do
			{
				var page = await service.ScanKeysAsync(connection, prefix + "*", 0, cursor, 10);
				Assert.That(page.IsSuccess, Is.True, page.IsFailed ? page.GetDomainError().Message : "");
				foreach (var item in page.Value.Keys) found.Add(item.Key);
				cursor = page.Value.NextCursor;
				Assert.That(++pages, Is.LessThan(1000));
			} while (cursor != null);
			Assert.That(multiplexer.GetServers().Length, Is.GreaterThanOrEqualTo(3));
			Assert.That(multiplexer.GetServers().Count(server => server.Keys(0, prefix + "*").Any()), Is.GreaterThanOrEqualTo(3));
			Assert.That(found, Has.Count.EqualTo(90));
		}
		finally
		{
			for (var i = 0; i < 90; i++) await db.KeyDeleteAsync(prefix + i);
		}
	}

	[Test]
	public async Task ScanKeys_ContinuesAcrossPagesAndUsesPatternAndDatabase()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(multiplexer));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Scan test"),
			new ConnectionHost("redis"), new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisKeyService(manager.Object, NullLogger<RedisKeyService>.Instance);
		var prefix = "redisgui-scan-test:" + Guid.NewGuid() + ":";
		var db = multiplexer.GetDatabase(1);
		try
		{
			for (var i = 0; i < 120; i++) await db.StringSetAsync(prefix + i, "value", TimeSpan.FromMinutes(5));
			await multiplexer.GetDatabase(0).StringSetAsync(prefix + "wrong-db", "value");
			string? cursor = null;
			var found = new System.Collections.Generic.HashSet<string>();
			var pages = 0;
			do
			{
				var page = await service.ScanKeysAsync(connection, prefix + "*", 1, cursor, 10);
				Assert.That(page.IsSuccess, Is.True, page.IsFailed ? page.GetDomainError().Message : "");
				foreach (var item in page.Value.Keys)
				{
					found.Add(item.Key);
					Assert.That(item.Type, Is.EqualTo("String"));
					Assert.That(item.TtlSeconds, Is.GreaterThan(0));
				}
				cursor = page.Value.NextCursor;
				if (pages == 0 && cursor != null)
				{
					var wrongPattern = await service.ScanKeysAsync(connection, "different:*", 1, cursor, 10);
					Assert.That(wrongPattern.IsFailed, Is.True);
				}
				Assert.That(++pages, Is.LessThan(1000));
			} while (cursor != null);
			Assert.That(pages, Is.GreaterThan(1));
			Assert.That(found, Has.Count.EqualTo(120));
			Assert.That(found, Does.Not.Contain(prefix + "wrong-db"));
			var invalid = await service.ScanKeysAsync(connection, prefix + "*", 1, "not-a-cursor", 10);
			Assert.That(invalid.IsFailed, Is.True);
			using var cancelled = new CancellationTokenSource();
			cancelled.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(async () =>
				await service.ScanKeysAsync(connection, prefix + "*", 1, null, 10, cancelled.Token));
		}
		finally
		{
			for (var i = 0; i < 120; i++) await db.KeyDeleteAsync(prefix + i);
			await multiplexer.GetDatabase(0).KeyDeleteAsync(prefix + "wrong-db");
		}
	}

	[Test]
	public async Task GetKeys_UsesRequestedDatabase()
	{
		var configuration = Environment.GetEnvironmentVariable("REDISGUI_TEST_REDIS");
		if (string.IsNullOrEmpty(configuration)) Assert.Ignore("Set REDISGUI_TEST_REDIS to run the Redis integration check.");
		using var multiplexer = await ConnectionMultiplexer.ConnectAsync(configuration!);
		var manager = new Mock<IRedisConnectionManager>();
		manager.Setup(m => m.GetConnection(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Ok(multiplexer));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Database test"),
			new ConnectionHost("redis"), new ConnectionPort(6379), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisKeyService(manager.Object, NullLogger<RedisKeyService>.Instance);
		var key = "redisgui-database-test:" + Guid.NewGuid();
		try
		{
			await multiplexer.GetDatabase(1).StringSetAsync(key, "value");
			var defaultKeys = await service.GetKeysAsync(connection);
			var selectedKeys = await service.GetKeysAsync(connection, database: 1);
			Assert.That(defaultKeys.IsSuccess, Is.True);
			Assert.That(selectedKeys.IsSuccess, Is.True);
			Assert.That(defaultKeys.Value, Does.Not.Contain(key));
			Assert.That(selectedKeys.Value, Does.Contain(key));
		}
		finally
		{
			await multiplexer.GetDatabase(1).KeyDeleteAsync(key);
		}
	}

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
			.ReturnsAsync(Result.Ok(multiplexer));
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
			Assert.That(collected.IsSuccess, Is.True, collected.IsFailed ? collected.GetDomainError().Message : string.Empty);
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
			.ReturnsAsync(Result.Ok(multiplexer));
		var connection = new AnonymousRedisConnection(Guid.NewGuid(), new ConnectionName("Fallback test"),
			new ConnectionHost("redis"), new ConnectionPort(endpoint.Port), new DatabaseNumber(0), ConnectionOwnerId.None());
		var service = new RedisLoadTestService(manager.Object, NullLogger<RedisLoadTestService>.Instance);
		var result = await service.RunLoadTestAsync(connection, new RedisLoadTestRequest(Concurrency: 2, OperationType: "mixed", OperationsCount: 10));
		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value.SuccessfulOperations, Is.GreaterThan(0));
	}
}

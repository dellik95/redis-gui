using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using RedisGUI.Domain.Errors;
using RedisGUI.Infrastructure.Redis;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Tests.Redis;

[TestFixture]
public class RedisConnectionManagerTests
{
	private sealed class TestRedisConnectionFactory : IRedisConnectionFactory
	{
		private int creationCount;
		public int CreationCount => creationCount;
		public List<ConfigurationOptions> CreatedOptions { get; } = [];
		public Func<ConfigurationOptions, Task<ConnectionMultiplexer>>? CustomConnect { get; set; }

		public async Task<ConnectionMultiplexer> ConnectAsync(ConfigurationOptions options, TextWriter? log = null)
		{
			Interlocked.Increment(ref creationCount);
			lock (CreatedOptions)
			{
				CreatedOptions.Add(options);
			}

			if (CustomConnect != null)
			{
				return await CustomConnect(options);
			}

			var testOptions = options.Clone();
			testOptions.AbortOnConnectFail = false;
			testOptions.ConnectTimeout = 100;
			return await ConnectionMultiplexer.ConnectAsync(testOptions, log);
		}
	}

	private static RedisConnectionManager CreateManager(
		TestRedisConnectionFactory factory,
		Action<RedisConnectionManagerOptions>? configureOptions = null)
	{
		var options = new RedisConnectionManagerOptions
		{
			ConnectionTimeout = TimeSpan.FromSeconds(2),
			IdleTimeout = TimeSpan.FromMinutes(5),
			EvictionInterval = TimeSpan.Zero, // disable automatic timer in tests unless needed
			DrainTimeout = TimeSpan.FromMilliseconds(50),
			EnableDockerFallback = false
		};
		configureOptions?.Invoke(options);

		return new RedisConnectionManager(
			NullLogger<RedisConnectionManager>.Instance,
			Options.Create(options),
			factory);
	}

	private static ConnectionFailedEventArgs CreateConnectionFailedEventArgs(ConnectionFailureType failureType, EndPoint? endPoint = null, Exception? exception = null)
	{
		var args = (ConnectionFailedEventArgs)RuntimeHelpers.GetUninitializedObject(typeof(ConnectionFailedEventArgs));
		typeof(ConnectionFailedEventArgs).GetField("<FailureType>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
			?.SetValue(args, failureType);
		typeof(ConnectionFailedEventArgs).GetField("<EndPoint>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
			?.SetValue(args, endPoint ?? new IPEndPoint(IPAddress.Loopback, 6379));
		typeof(ConnectionFailedEventArgs).GetField("<Exception>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
			?.SetValue(args, exception);
		return args;
	}

	// 1. Одночасні запити до одного Redis server створюють лише один multiplexer.
	[Test]
	public async Task ConcurrentRequests_ToSameServer_CreatesSingleMultiplexer()
	{
		var factory = new TestRedisConnectionFactory();
		var tcs = new TaskCompletionSource<bool>();

		factory.CustomConnect = async opt =>
		{
			await tcs.Task;
			var testOpt = opt.Clone();
			testOpt.AbortOnConnectFail = false;
			testOpt.ConnectTimeout = 100;
			return await ConnectionMultiplexer.ConnectAsync(testOpt);
		};

		using var manager = CreateManager(factory);

		// Launch 10 concurrent requests for the same server
		var tasks = Enumerable.Range(0, 10)
			.Select(_ => manager.GetConnection("127.0.0.1:6389,abortConnect=false"))
			.ToList();

		// Release the factory connect
		tcs.SetResult(true);

		var results = await Task.WhenAll(tasks);

		results.Should().OnlyContain(r => r.IsSuccess);
		factory.CreationCount.Should().Be(1);

		ConnectionMultiplexer firstMux = results[0].Value;
		foreach (var r in results)
		{
			ReferenceEquals(r.Value, firstMux).Should().BeTrue();
		}
	}

	// 2. Одночасні запити до різних Redis servers створюють незалежні multiplexers.
	[Test]
	public async Task ConcurrentRequests_ToDifferentServers_CreatesIndependentMultiplexers()
	{
		var factory = new TestRedisConnectionFactory();
		using var manager = CreateManager(factory);

		var task1 = manager.GetConnection("127.0.0.1:6381,abortConnect=false");
		var task2 = manager.GetConnection("127.0.0.1:6382,abortConnect=false");

		var results = await Task.WhenAll(task1, task2);

		results[0].IsSuccess.Should().BeTrue();
		results[1].IsSuccess.Should().BeTrue();

		factory.CreationCount.Should().Be(2);
		ReferenceEquals(results[0].Value, results[1].Value).Should().BeFalse();
	}

	// 3. Повторний запит використовує вже створений multiplexer.
	[Test]
	public async Task SubsequentRequest_ReusesAlreadyCreatedMultiplexer()
	{
		var factory = new TestRedisConnectionFactory();
		using var manager = CreateManager(factory);

		var result1 = await manager.GetConnection("127.0.0.1:6383,abortConnect=false");
		var result2 = await manager.GetConnection("127.0.0.1:6383,abortConnect=false");

		result1.IsSuccess.Should().BeTrue();
		result2.IsSuccess.Should().BeTrue();

		factory.CreationCount.Should().Be(1);
		ReferenceEquals(result1.Value, result2.Value).Should().BeTrue();
	}

	// 4. Помилка initialization не залишає некоректний entry у registry.
	[Test]
	public async Task InitializationError_DoesNotLeaveIncorrectEntryInRegistry()
	{
		var factory = new TestRedisConnectionFactory
		{
			CustomConnect = _ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Simulated network failure")
		};
		using var manager = CreateManager(factory);

		var result = await manager.GetConnection("127.0.0.1:6384");

		result.IsFailure.Should().BeTrue();
		manager.GetStatistics().TotalCachedConnections.Should().Be(0);
		manager.GetConnectionStates().Should().BeEmpty();
	}

	// 5. Після невдалого initialization можливе повторне підключення.
	[Test]
	public async Task FailedInitialization_AllowsSubsequentReconnection()
	{
		int attempts = 0;
		var factory = new TestRedisConnectionFactory
		{
			CustomConnect = async opt =>
			{
				if (Interlocked.Increment(ref attempts) == 1)
				{
					throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "First attempt failed");
				}

				var testOpt = opt.Clone();
				testOpt.AbortOnConnectFail = false;
				testOpt.ConnectTimeout = 100;
				return await ConnectionMultiplexer.ConnectAsync(testOpt);
			}
		};
		using var manager = CreateManager(factory);

		var firstResult = await manager.GetConnection("127.0.0.1:6385");
		firstResult.IsFailure.Should().BeTrue();

		var secondResult = await manager.GetConnection("127.0.0.1:6385,abortConnect=false");
		secondResult.IsSuccess.Should().BeTrue();

		factory.CreationCount.Should().Be(2);
		manager.GetStatistics().TotalCachedConnections.Should().Be(1);
	}

	// 6. Cancellation одного caller не впливає на інших callers.
	[Test]
	public async Task CancellationOfOneCaller_DoesNotAffectOtherCallers()
	{
		var factory = new TestRedisConnectionFactory();
		var connectStartedTcs = new TaskCompletionSource<bool>();
		var connectCompleteTcs = new TaskCompletionSource<bool>();

		factory.CustomConnect = async opt =>
		{
			connectStartedTcs.TrySetResult(true);
			await connectCompleteTcs.Task;
			var testOpt = opt.Clone();
			testOpt.AbortOnConnectFail = false;
			testOpt.ConnectTimeout = 100;
			return await ConnectionMultiplexer.ConnectAsync(testOpt);
		};

		using var manager = CreateManager(factory);

		using var ctsCaller1 = new CancellationTokenSource();
		var taskCaller1 = manager.GetConnection("127.0.0.1:6386,abortConnect=false", ctsCaller1.Token);
		var taskCaller2 = manager.GetConnection("127.0.0.1:6386,abortConnect=false", CancellationToken.None);

		// Wait until connection creation is in-flight
		await connectStartedTcs.Task;

		// Cancel caller 1
		ctsCaller1.Cancel();

		var resultCaller1 = await taskCaller1;
		resultCaller1.IsFailure.Should().BeTrue();
		resultCaller1.Error.Code.Should().Be(DomainErrors.ConnectionManager.Cancelled.Code);

		// Complete connect for caller 2
		connectCompleteTcs.SetResult(true);

		var resultCaller2 = await taskCaller2;
		resultCaller2.IsSuccess.Should().BeTrue();
		factory.CreationCount.Should().Be(1);
	}

	// 7. Timeout initialization не призводить до витоку ресурсів.
	[Test]
	public async Task TimeoutInitialization_CleansUpLateArrivingMultiplexerWithoutResourceLeak()
	{
		var factory = new TestRedisConnectionFactory();
		var lateMuxTcs = new TaskCompletionSource<ConnectionMultiplexer>();

		factory.CustomConnect = async opt =>
		{
			return await lateMuxTcs.Task;
		};

		using var manager = CreateManager(factory, opt =>
		{
			opt.ConnectionTimeout = TimeSpan.FromMilliseconds(50);
		});

		var result = await manager.GetConnection("127.0.0.1:6387,abortConnect=false");

		result.IsFailure.Should().BeTrue();

		// Create a real test multiplexer and release it as late-arriving
		var testOpt = ConfigurationOptions.Parse("127.0.0.1:6387,abortConnect=false,connectTimeout=100");
		var lateMux = await ConnectionMultiplexer.ConnectAsync(testOpt);

		lateMuxTcs.SetResult(lateMux);

		// Wait briefly for the safety continuation to execute
		await Task.Delay(100);

		manager.GetStatistics().TotalCachedConnections.Should().Be(0);
	}

	// 8. Тимчасова втрата з'єднання не спричиняє передчасного disposal.
	[Test]
	public async Task TemporaryConnectionLoss_DoesNotCausePrematureDisposal()
	{
		var factory = new TestRedisConnectionFactory();
		using var manager = CreateManager(factory);

		var result = await manager.GetConnection("server-temp-loss", "127.0.0.1:6388,abortConnect=false");
		result.IsSuccess.Should().BeTrue();

		var states = manager.GetConnectionStates();
		states.Should().ContainKey("server-temp-loss");

		// Simulate ConnectionFailed event via ConnectionEntry
		var failedArgs = CreateConnectionFailedEventArgs(ConnectionFailureType.SocketFailure);
		var entry = manager.GetConnectionStates()["server-temp-loss"];

		// Manually fire connection failed on the manager's connection
		// Since RedisConnectionManager exposes internal ConnectionEntry:
		var internalEntryField = manager.GetType().GetField("connections", BindingFlags.Instance | BindingFlags.NonPublic);
		var internalDict = (System.Collections.IDictionary)internalEntryField!.GetValue(manager)!;
		var internalEntry = internalDict["server-temp-loss"]!;
		var onFailedMethod = internalEntry.GetType().GetMethod("OnConnectionFailed", BindingFlags.Instance | BindingFlags.NonPublic)!;
		onFailedMethod.Invoke(internalEntry, [null, failedArgs]);

		// Verify state updated to Disconnected without removing connection from registry
		manager.GetConnectionStates()["server-temp-loss"].State.Should().Be(RedisConnectionState.Disconnected);
		manager.GetStatistics().TotalCachedConnections.Should().Be(1);

		// Subsequent GetConnection call still returns the multiplexer for automatic reconnect
		var reuseResult = await manager.GetConnection("server-temp-loss", "127.0.0.1:6388,abortConnect=false");
		reuseResult.IsSuccess.Should().BeTrue();
		ReferenceEquals(reuseResult.Value, result.Value).Should().BeTrue();
	}

	// 9. Connection restoration коректно оновлює runtime state.
	[Test]
	public async Task ConnectionRestoration_CorrectlyUpdatesRuntimeState()
	{
		var factory = new TestRedisConnectionFactory();
		using var manager = CreateManager(factory);

		var result = await manager.GetConnection("server-restore", "127.0.0.1:6389,abortConnect=false");
		result.IsSuccess.Should().BeTrue();

		var internalEntryField = manager.GetType().GetField("connections", BindingFlags.Instance | BindingFlags.NonPublic);
		var internalDict = (System.Collections.IDictionary)internalEntryField!.GetValue(manager)!;
		var internalEntry = internalDict["server-restore"]!;

		// 1. Fire ConnectionFailed
		var failedArgs = CreateConnectionFailedEventArgs(ConnectionFailureType.SocketFailure);
		var onFailedMethod = internalEntry.GetType().GetMethod("OnConnectionFailed", BindingFlags.Instance | BindingFlags.NonPublic)!;
		onFailedMethod.Invoke(internalEntry, [null, failedArgs]);

		manager.GetConnectionStates()["server-restore"].State.Should().Be(RedisConnectionState.Disconnected);

		// 2. Fire ConnectionRestored
		var restoredArgs = CreateConnectionFailedEventArgs(ConnectionFailureType.None);
		internalEntry.GetType().GetMethod("OnConnectionRestored", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(internalEntry, [null, restoredArgs]);

		var state = manager.GetConnectionStates()["server-restore"];
		state.State.Should().Be(RedisConnectionState.Connected);
		state.LastConnectedAt.Should().NotBeNull();
		manager.GetStatistics().ConnectionRestorationCount.Should().Be(1);
	}

	// 10. Concurrent GetConnection та eviction не призводять до використання disposed multiplexer.
	[Test]
	public async Task ConcurrentGetConnectionAndEviction_DoesNotReturnDisposedMultiplexer()
	{
		var factory = new TestRedisConnectionFactory();
		using var manager = CreateManager(factory, opt =>
		{
			opt.IdleTimeout = TimeSpan.FromMilliseconds(20);
			opt.DrainTimeout = TimeSpan.FromMilliseconds(50);
		});

		var initialResult = await manager.GetConnection("server-evict-race", "127.0.0.1:6390,abortConnect=false");
		initialResult.IsSuccess.Should().BeTrue();

		// Wait past idle timeout
		await Task.Delay(40);

		// Concurrently trigger eviction and GetConnection
		var evictTask = Task.Run(() => manager.RunEvictionCycle());
		var getTask = Task.Run(() => manager.GetConnection("server-evict-race", "127.0.0.1:6390,abortConnect=false"));

		await Task.WhenAll(evictTask, getTask);

		var getResult = await getTask;
		getResult.IsSuccess.Should().BeTrue();
		// The returned multiplexer must be active and not thrown ObjectDisposedException
		Assert.DoesNotThrow(() => { _ = getResult.Value.IsConnected; });
	}

	// 11. Concurrent Dispose/DisposeAsync не призводять до подвійного звільнення ресурсів.
	[Test]
	public async Task ConcurrentDisposeAndDisposeAsync_AreIdempotentAndDoNotDoubleDispose()
	{
		var factory = new TestRedisConnectionFactory();
		var manager = CreateManager(factory);

		await manager.GetConnection("127.0.0.1:6391,abortConnect=false");

		// Run concurrent Dispose and DisposeAsync calls
		var task1 = Task.Run(() => manager.Dispose());
		var task2 = Task.Run(async () => await manager.DisposeAsync());
		var task3 = Task.Run(() => manager.Dispose());

		Assert.DoesNotThrowAsync(async () => await Task.WhenAll(task1, task2, task3));
	}

	// 12. Після початку disposal нові connections не створюються.
	[Test]
	public async Task AfterDisposalStarted_NewConnectionsCannotBeCreated()
	{
		var factory = new TestRedisConnectionFactory();
		var manager = CreateManager(factory);

		manager.Dispose();

		var result = await manager.GetConnection("127.0.0.1:6392,abortConnect=false");

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be(DomainErrors.ConnectionManager.Cancelled.Code);
		factory.CreationCount.Should().Be(0);
	}

	// 13. Idle connections видаляються відповідно до налаштованої eviction policy.
	[Test]
	public async Task IdleConnections_AreEvictedAccordingToPolicy()
	{
		var factory = new TestRedisConnectionFactory();
		using var manager = CreateManager(factory, opt =>
		{
			opt.IdleTimeout = TimeSpan.FromMilliseconds(50);
			opt.DrainTimeout = TimeSpan.FromMilliseconds(10);
		});

		var result = await manager.GetConnection("idle-server", "127.0.0.1:6393,abortConnect=false");
		result.IsSuccess.Should().BeTrue();
		manager.GetStatistics().TotalCachedConnections.Should().Be(1);

		// Wait past idle timeout
		await Task.Delay(100);

		manager.RunEvictionCycle();

		manager.GetStatistics().TotalCachedConnections.Should().Be(0);
		manager.GetStatistics().EvictionCount.Should().Be(1);
	}

	// 14. Активні connections не видаляються під час виконання операцій.
	[Test]
	public async Task ActiveConnections_AreNotEvictedDuringExecution()
	{
		var factory = new TestRedisConnectionFactory();
		using var manager = CreateManager(factory, opt =>
		{
			opt.IdleTimeout = TimeSpan.FromMilliseconds(50);
			opt.DrainTimeout = TimeSpan.FromMilliseconds(10);
		});

		var result = await manager.GetConnection("active-server", "127.0.0.1:6394,abortConnect=false");
		result.IsSuccess.Should().BeTrue();

		// Track active operation scope
		using (var scope = manager.TrackActiveOperation("active-server"))
		{
			scope.Should().NotBeNull();

			// Wait past idle timeout while operation is active
			await Task.Delay(100);

			manager.RunEvictionCycle();

			// Active connection must NOT be evicted!
			manager.GetStatistics().TotalCachedConnections.Should().Be(1);
		}

		// Once the active operation scope is disposed and past idle timeout:
		await Task.Delay(60);
		manager.RunEvictionCycle();

		manager.GetStatistics().TotalCachedConnections.Should().Be(0);
	}

	// 15. Зміна Redis server configuration призводить до контрольованої заміни multiplexer.
	[Test]
	public async Task ConfigurationChange_ReplacesConnectionControlled()
	{
		var factory = new TestRedisConnectionFactory();
		using var manager = CreateManager(factory);

		var result1 = await manager.GetConnection("srv1", "127.0.0.1:6395,password=secret1,abortConnect=false");
		result1.IsSuccess.Should().BeTrue();
		factory.CreationCount.Should().Be(1);

		// Call with changed configuration (different password)
		var result2 = await manager.GetConnection("srv1", "127.0.0.1:6395,password=secret2,abortConnect=false");
		result2.IsSuccess.Should().BeTrue();

		// A new multiplexer must have been created to replace the old one
		factory.CreationCount.Should().Be(2);
		ReferenceEquals(result1.Value, result2.Value).Should().BeFalse();
	}

	// 16. Fallback hostname працює лише у дозволених сценаріях.
	[Test]
	public async Task FallbackHostname_WorksOnlyWhenExplicitlyAllowed()
	{
		// Scenario A: EnableDockerFallback is false (default)
		var factoryA = new TestRedisConnectionFactory
		{
			CustomConnect = opt =>
			{
				if (opt.EndPoints[0].ToString()!.Contains("redis", StringComparison.OrdinalIgnoreCase))
				{
					throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Host not found: redis");
				}
				var testOpt = opt.Clone();
				testOpt.AbortOnConnectFail = false;
				testOpt.ConnectTimeout = 100;
				return ConnectionMultiplexer.ConnectAsync(testOpt);
			}
		};
		using var managerA = CreateManager(factoryA, opt => opt.EnableDockerFallback = false);

		var resultA = await managerA.GetConnection("redis:6379");
		resultA.IsFailure.Should().BeTrue();
		factoryA.CreatedOptions.Should().ContainSingle(o => o.EndPoints[0].ToString()!.Contains("redis", StringComparison.OrdinalIgnoreCase));

		// Scenario B: EnableDockerFallback is true
		var factoryB = new TestRedisConnectionFactory
		{
			CustomConnect = opt =>
			{
				string ep = opt.EndPoints[0].ToString()!;
				if (ep.Contains("redis", StringComparison.OrdinalIgnoreCase))
				{
					throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, CommandFlags.None, "Host not found: redis");
				}
				// Fallback to 127.0.0.1 succeeds
				var testOpt = opt.Clone();
				testOpt.AbortOnConnectFail = false;
				testOpt.ConnectTimeout = 100;
				return ConnectionMultiplexer.ConnectAsync(testOpt);
			}
		};
		using var managerB = CreateManager(factoryB, opt => opt.EnableDockerFallback = true);

		var resultB = await managerB.GetConnection("redis:6379");
		resultB.IsSuccess.Should().BeTrue();
		factoryB.CreationCount.Should().Be(2);
		factoryB.CreatedOptions.Any(o => o.EndPoints[0].ToString()!.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
	}

	// Additional test: RedisConfigurationSanitizer masks credentials properly
	[Test]
	public void Sanitizer_MasksPasswordsAndDerivesStableKeys()
	{
		string raw = "server1:6379,password=SuperSecretPassword123,ssl=true";
		string sanitized = RedisConfigurationSanitizer.Sanitize(raw);

		sanitized.Should().NotContain("SuperSecretPassword123");
		sanitized.Should().Contain("password=******");

		string key = RedisConfigurationSanitizer.GetStableKey(null, raw);
		key.Should().Be("server1:6379");
	}
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedisGUI.Domain.Errors;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Thread-safe manager for cached, shared <see cref="ConnectionMultiplexer"/> instances.
/// Implements single-flight initialization, safe lifecycle management, bounded cache eviction, and detailed observability.
/// </summary>
public class RedisConnectionManager : IRedisConnectionManager
{
	private readonly ConcurrentDictionary<string, ConnectionEntry> connections = new(StringComparer.OrdinalIgnoreCase);
	private readonly ILogger logger;
	private readonly RedisConnectionManagerOptions options;
	private readonly IRedisConnectionFactory connectionFactory;
	private readonly CancellationTokenSource shutdownCts = new();
	private readonly Timer? evictionTimer;

	private int disposeState; // 0 = active, 1 = disposing, 2 = disposed
	private long creationCount;
	private long failureCount;
	private long restorationCount;
	private long evictionCount;

	public RedisConnectionManager(
		ILogger<RedisConnectionManager> logger,
		IOptions<RedisConnectionManagerOptions> options,
		IRedisConnectionFactory connectionFactory)
	{
		this.logger = logger;
		this.options = options.Value;
		this.connectionFactory = connectionFactory;

		if (this.options.EvictionInterval > TimeSpan.Zero)
		{
			this.evictionTimer = new Timer(
				_ => RunEvictionCycle(),
				null,
				this.options.EvictionInterval,
				this.options.EvictionInterval);
		}
	}

	/// <inheritdoc />
	public Task<Result<ConnectionMultiplexer>> GetConnection(
		string configuration,
		CancellationToken cancellationToken = default)
	{
		return GetConnection(string.Empty, configuration, cancellationToken);
	}

	/// <inheritdoc />
	public async Task<Result<ConnectionMultiplexer>> GetConnection(
		string key,
		string configuration,
		CancellationToken cancellationToken = default)
	{
		if (disposeState != 0)
		{
			return Result.Failure<ConnectionMultiplexer>(DomainErrors.ConnectionManager.Cancelled);
		}

		if (string.IsNullOrWhiteSpace(configuration))
		{
			return Result.Failure<ConnectionMultiplexer>(DomainErrors.Connection.InvalidConfiguration);
		}

		string stableKey = RedisConfigurationSanitizer.GetStableKey(key, configuration);
		string sanitizedKey = RedisConfigurationSanitizer.Sanitize(stableKey);
		string fingerprint = RedisConfigurationSanitizer.ComputeFingerprint(configuration);

		while (disposeState == 0)
		{
			if (connections.TryGetValue(stableKey, out ConnectionEntry? existingEntry))
			{
				if (existingEntry.ConfigurationFingerprint != fingerprint)
				{
					logger.LogInformation("Redis configuration changed for server {ServerKey}. Replacing existing connection", sanitizedKey);
					EvictAndRetire(existingEntry, "Configuration changed");
				}
				else if (existingEntry.IsRetired)
				{
					// Entry is being retired, retry to acquire a fresh entry
					connections.TryRemove(new KeyValuePair<string, ConnectionEntry>(stableKey, existingEntry));
				}
				else
				{
					return await AwaitOrReturnMultiplexerAsync(existingEntry, cancellationToken);
				}
			}

			// Check capacity before adding new entry
			if (connections.Count >= options.MaxConnections)
			{
				RunEvictionCycle();
			}

			var newEntry = new ConnectionEntry(stableKey, sanitizedKey, configuration, fingerprint, this);
			ConnectionEntry actualEntry = connections.GetOrAdd(stableKey, newEntry);

			if (actualEntry.IsRetired)
			{
				connections.TryRemove(new KeyValuePair<string, ConnectionEntry>(stableKey, actualEntry));
				continue;
			}

			return await AwaitOrReturnMultiplexerAsync(actualEntry, cancellationToken);
		}

		return Result.Failure<ConnectionMultiplexer>(DomainErrors.ConnectionManager.Cancelled);
	}

	/// <inheritdoc />
	public async Task InvalidateConnectionAsync(
		string key,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}

		if (connections.TryRemove(key, out ConnectionEntry? entry))
		{
			entry.IsRetired = true;
			entry.State = RedisConnectionState.Disposing;
			logger.LogInformation("Connection explicitly invalidated for server {ServerKey}", entry.SanitizedKey);
			await RetireAndDisposeEntryAsync(entry);
		}
	}

	/// <inheritdoc />
	public RedisConnectionManagerStatistics GetStatistics()
	{
		int total = connections.Count;
		int active = connections.Values.Count(e => e.ActiveOperations > 0 || (e.Multiplexer != null && e.State == RedisConnectionState.Connected));

		return new RedisConnectionManagerStatistics(
			TotalCachedConnections: total,
			ActiveConnections: active,
			ConnectionCreationCount: Interlocked.Read(ref creationCount),
			ConnectionFailureCount: Interlocked.Read(ref failureCount),
			ConnectionRestorationCount: Interlocked.Read(ref restorationCount),
			EvictionCount: Interlocked.Read(ref evictionCount)
		);
	}

	/// <inheritdoc />
	public IReadOnlyDictionary<string, RedisConnectionInfo> GetConnectionStates()
	{
		var dict = new Dictionary<string, RedisConnectionInfo>(connections.Count, StringComparer.OrdinalIgnoreCase);
		foreach (var (key, entry) in connections)
		{
			dict[key] = new RedisConnectionInfo(
				Key: entry.Key,
				SanitizedEndpoint: entry.SanitizedKey,
				State: entry.State,
				LastConnectedAt: entry.LastConnectedAt,
				LastDisconnectedAt: entry.LastDisconnectedAt,
				LastUsedAt: entry.LastUsedAt,
				InitializationDuration: entry.InitializationDuration,
				LastFailure: entry.LastFailure,
				IsConnected: entry.Multiplexer?.IsConnected ?? false,
				ActiveOperations: entry.ActiveOperations
			);
		}
		return dict;
	}

	/// <summary>
	/// Coordinates single-flight connection initialization or returns the established multiplexer.
	/// </summary>
	private async Task<Result<ConnectionMultiplexer>> AwaitOrReturnMultiplexerAsync(
		ConnectionEntry entry,
		CancellationToken cancellationToken)
	{
		Task<ConnectionMultiplexer> initTaskToWait;

		lock (entry.InitLock)
		{
			if (entry.Multiplexer != null && entry.State != RedisConnectionState.Faulted)
			{
				entry.LastUsedAt = DateTimeOffset.UtcNow;
				return Result.Success(entry.Multiplexer);
			}

			if (entry.InitTask == null || entry.InitTask.IsFaulted || entry.InitTask.IsCanceled)
			{
				entry.State = RedisConnectionState.Connecting;
				var initCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownCts.Token);
				initCts.CancelAfter(options.ConnectionTimeout);
				entry.InitTask = InitializeMultiplexerAsync(entry, entry.Configuration, initCts);
			}

			initTaskToWait = entry.InitTask;
		}

		try
		{
			ConnectionMultiplexer mux = await initTaskToWait.WaitAsync(cancellationToken);
			entry.LastUsedAt = DateTimeOffset.UtcNow;
			return Result.Success(mux);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return Result.Failure<ConnectionMultiplexer>(DomainErrors.ConnectionManager.Cancelled);
		}
		catch (RedisConnectionException ex)
		{
			logger.LogError(ex, "Failed to establish Redis connection for server {ServerKey}", entry.SanitizedKey);
			return Result.Failure<ConnectionMultiplexer>(DomainErrors.Connection.ConnectionNotEstablished);
		}
		catch (TimeoutException ex)
		{
			logger.LogError(ex, "Timeout establishing Redis connection for server {ServerKey}", entry.SanitizedKey);
			return Result.Failure<ConnectionMultiplexer>(DomainErrors.Connection.ConnectionNotEstablished);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unexpected error establishing Redis connection for server {ServerKey}", entry.SanitizedKey);
			return Result.Failure<ConnectionMultiplexer>(DomainErrors.ConnectionManager.UnexpectedError);
		}
	}

	/// <summary>
	/// Performs actual connection initialization with timeout, fallback handling, and resource leak protection.
	/// </summary>
	private async Task<ConnectionMultiplexer> InitializeMultiplexerAsync(
		ConnectionEntry entry,
		string configuration,
		CancellationTokenSource initCts)
	{
		var sw = Stopwatch.StartNew();
		logger.LogInformation("Redis connection initialization started for server {ServerKey}", entry.SanitizedKey);
		Interlocked.Increment(ref creationCount);

		Task<ConnectionMultiplexer>? connectTask = null;
		ConnectionMultiplexer? multiplexer = null;

		try
		{
			ConfigurationOptions configOptions = ConfigurationOptions.Parse(configuration);
			if (configOptions.ConnectTimeout <= 0)
			{
				configOptions.ConnectTimeout = (int)options.ConnectionTimeout.TotalMilliseconds;
			}

			connectTask = connectionFactory.ConnectAsync(configOptions);

			// Attach background safety continuation to ensure late-completing connections are disposed if abandoned
			_ = connectTask.ContinueWith(async t =>
			{
				if (t.IsCompletedSuccessfully && (entry.IsRetired || disposeState != 0 || entry.Multiplexer != t.Result))
				{
					try
					{
						await t.Result.CloseAsync(allowCommandsToComplete: false);
						await t.Result.DisposeAsync();
					}
					catch { }
				}
			}, TaskScheduler.Default);

			multiplexer = await connectTask.WaitAsync(initCts.Token);

			if (disposeState != 0 || entry.IsRetired)
			{
				if (multiplexer != null)
				{
					await entry.CloseAndDisposeMultiplexerInternalAsync(multiplexer);
				}
				throw new ObjectDisposedException(nameof(RedisConnectionManager));
			}

			sw.Stop();
			entry.InitializationDuration = sw.Elapsed;
			entry.Multiplexer = multiplexer;
			entry.State = multiplexer.IsConnected ? RedisConnectionState.Connected : RedisConnectionState.Disconnected;
			entry.LastConnectedAt = multiplexer.IsConnected ? DateTimeOffset.UtcNow : null;

			multiplexer.ConnectionFailed += entry.OnConnectionFailed;
			multiplexer.ConnectionRestored += entry.OnConnectionRestored;
			multiplexer.ErrorMessage += entry.OnErrorMessage;

			logger.LogInformation(
				"Redis connection established for server {ServerKey} in {DurationMs}ms",
				entry.SanitizedKey,
				sw.ElapsedMilliseconds);

			return multiplexer;
		}
		catch (Exception ex)
		{
			sw.Stop();
			entry.State = RedisConnectionState.Faulted;
			entry.LastFailure = ex.Message;
			Interlocked.Increment(ref failureCount);

			if (options.EnableDockerFallback && IsDockerRedisHost(configuration))
			{
				try
				{
					return await AttemptDockerFallbackAsync(entry, configuration, initCts, sw);
				}
				catch (Exception fallbackEx)
				{
					logger.LogWarning(fallbackEx, "Redis fallback to 127.0.0.1 failed for server {ServerKey}", entry.SanitizedKey);
				}
			}

			logger.LogError(ex, "Redis connection initialization failed for server {ServerKey}", entry.SanitizedKey);

			if (multiplexer != null)
			{
				await entry.CloseAndDisposeMultiplexerInternalAsync(multiplexer);
			}

			connections.TryRemove(new KeyValuePair<string, ConnectionEntry>(entry.Key, entry));
			throw;
		}
		finally
		{
			initCts.Dispose();
		}
	}

	private async Task<ConnectionMultiplexer> AttemptDockerFallbackAsync(
		ConnectionEntry entry,
		string configuration,
		CancellationTokenSource initCts,
		Stopwatch sw)
	{
		string fallbackConfig = configuration
			.Replace("redis:", "127.0.0.1:", StringComparison.OrdinalIgnoreCase)
			.Replace("redis,", "127.0.0.1,", StringComparison.OrdinalIgnoreCase);

		logger.LogWarning(
			"Failed to connect to docker host 'redis'. Docker fallback is enabled, attempting fallback to '{FallbackConfig}'",
			RedisConfigurationSanitizer.Sanitize(fallbackConfig));

		ConfigurationOptions fallbackOptions = ConfigurationOptions.Parse(fallbackConfig);
		if (fallbackOptions.ConnectTimeout <= 0)
		{
			fallbackOptions.ConnectTimeout = (int)options.ConnectionTimeout.TotalMilliseconds;
		}

		Task<ConnectionMultiplexer> fallbackTask = connectionFactory.ConnectAsync(fallbackOptions);

		_ = fallbackTask.ContinueWith(async t =>
		{
			if (t.IsCompletedSuccessfully && (entry.IsRetired || disposeState != 0 || entry.Multiplexer != t.Result))
			{
				try
				{
					await t.Result.CloseAsync(allowCommandsToComplete: false);
					await t.Result.DisposeAsync();
				}
				catch { }
			}
		}, TaskScheduler.Default);

		ConnectionMultiplexer fallbackMux = await fallbackTask.WaitAsync(initCts.Token);

		if (disposeState != 0 || entry.IsRetired)
		{
			await entry.CloseAndDisposeMultiplexerInternalAsync(fallbackMux);
			throw new ObjectDisposedException(nameof(RedisConnectionManager));
		}

		sw.Stop();
		entry.InitializationDuration = sw.Elapsed;
		entry.Multiplexer = fallbackMux;
		entry.State = fallbackMux.IsConnected ? RedisConnectionState.Connected : RedisConnectionState.Disconnected;
		entry.LastConnectedAt = fallbackMux.IsConnected ? DateTimeOffset.UtcNow : null;

		fallbackMux.ConnectionFailed += entry.OnConnectionFailed;
		fallbackMux.ConnectionRestored += entry.OnConnectionRestored;
		fallbackMux.ErrorMessage += entry.OnErrorMessage;

		logger.LogInformation("Redis connection established via Docker fallback for server {ServerKey}", entry.SanitizedKey);
		return fallbackMux;
	}

	private static bool IsDockerRedisHost(string configuration)
	{
		return configuration.StartsWith("redis:", StringComparison.OrdinalIgnoreCase)
			|| configuration.StartsWith("redis,", StringComparison.OrdinalIgnoreCase)
			|| configuration.Contains("@redis:", StringComparison.OrdinalIgnoreCase)
			|| configuration.Contains("@redis,", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Performs periodic eviction of idle connections and enforces bounded capacity via LRU.
	/// </summary>
	internal void RunEvictionCycle()
	{
		if (disposeState != 0)
		{
			return;
		}

		DateTimeOffset now = DateTimeOffset.UtcNow;

		// 1. Evict idle connections exceeding IdleTimeout
		foreach (var pair in connections)
		{
			ConnectionEntry entry = pair.Value;
			if (entry.ActiveOperations > 0 || SafeGetOutstanding(entry.Multiplexer) > 0)
			{
				continue;
			}

			if (now - entry.LastUsedAt >= options.IdleTimeout)
			{
				EvictAndRetire(entry, "IdleTimeout exceeded");
			}
		}

		// 2. Enforce MaxConnections capacity (evict LRU idle connections)
		if (connections.Count > options.MaxConnections)
		{
			var eligibleCandidates = connections.Values
				.Where(e => e.ActiveOperations == 0 && SafeGetOutstanding(e.Multiplexer) == 0)
				.OrderBy(e => e.LastUsedAt)
				.ToList();

			foreach (ConnectionEntry candidate in eligibleCandidates)
			{
				if (connections.Count <= options.MaxConnections)
				{
					break;
				}

				EvictAndRetire(candidate, "MaxConnections limit reached (LRU)");
			}
		}
	}

	private void EvictAndRetire(ConnectionEntry entry, string reason)
	{
		if (connections.TryRemove(new KeyValuePair<string, ConnectionEntry>(entry.Key, entry)))
		{
			entry.IsRetired = true;
			entry.State = RedisConnectionState.Disposing;
			Interlocked.Increment(ref evictionCount);
			logger.LogInformation("Redis connection evicted for server {ServerKey}. Reason: {Reason}", entry.SanitizedKey, reason);
			_ = RetireAndDisposeEntryAsync(entry);
		}
	}

	private async Task RetireAndDisposeEntryAsync(ConnectionEntry entry)
	{
		try
		{
			if (options.DrainTimeout > TimeSpan.Zero && disposeState == 0)
			{
				await Task.Delay(options.DrainTimeout, shutdownCts.Token).ConfigureAwait(false);
			}
		}
		catch (OperationCanceledException) { }
		finally
		{
			await entry.DisposeMultiplexerAsync();
			logger.LogDebug("Redis connection disposed for server {ServerKey}", entry.SanitizedKey);
		}
	}

	private static int SafeGetOutstanding(ConnectionMultiplexer? multiplexer)
	{
		if (multiplexer == null)
		{
			return 0;
		}

		try
		{
			ServerCounters counters = multiplexer.GetCounters();
			return (int)counters.TotalOutstanding;
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>
	/// Tracks an active in-flight operation on the connection with the given key to prevent eviction during execution.
	/// </summary>
	internal IDisposable? TrackActiveOperation(string key)
	{
		if (connections.TryGetValue(key, out ConnectionEntry? entry))
		{
			return entry.TrackOperation();
		}
		return null;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (Interlocked.CompareExchange(ref disposeState, 1, 0) != 0)
		{
			return;
		}

		shutdownCts.Cancel();

		if (evictionTimer != null)
		{
			await evictionTimer.DisposeAsync();
		}

		ConnectionEntry[] entries = connections.Values.ToArray();
		connections.Clear();

		foreach (ConnectionEntry entry in entries)
		{
			entry.IsRetired = true;
			entry.State = RedisConnectionState.Disposing;
			await entry.DisposeMultiplexerAsync();
			logger.LogDebug("Redis connection disposed for server {ServerKey}", entry.SanitizedKey);
		}

		shutdownCts.Dispose();
		disposeState = 2;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.CompareExchange(ref disposeState, 1, 0) != 0)
		{
			return;
		}

		shutdownCts.Cancel();
		evictionTimer?.Dispose();

		ConnectionEntry[] entries = connections.Values.ToArray();
		connections.Clear();

		foreach (ConnectionEntry entry in entries)
		{
			entry.IsRetired = true;
			entry.State = RedisConnectionState.Disposing;
			entry.DisposeMultiplexerSync();
			logger.LogDebug("Redis connection disposed for server {ServerKey}", entry.SanitizedKey);
		}

		shutdownCts.Dispose();
		disposeState = 2;
	}

	/// <summary>
	/// Represents an individual registered Redis server connection entry in the manager.
	/// </summary>
	internal sealed class ConnectionEntry
	{
		public string Key { get; }
		public string SanitizedKey { get; }
		public string Configuration { get; }
		public string ConfigurationFingerprint { get; }
		public RedisConnectionState State { get; set; }
		public DateTimeOffset? LastConnectedAt { get; set; }
		public DateTimeOffset? LastDisconnectedAt { get; set; }
		public DateTimeOffset LastUsedAt { get; set; }
		public TimeSpan? InitializationDuration { get; set; }
		public string? LastFailure { get; set; }
		public int ActiveOperations;
		public volatile bool IsRetired;

		public ConnectionMultiplexer? Multiplexer { get; set; }

		internal readonly object InitLock = new();
		internal Task<ConnectionMultiplexer>? InitTask;

		private readonly RedisConnectionManager manager;
		private int disposalState;

		public ConnectionEntry(
			string key,
			string sanitizedKey,
			string configuration,
			string configurationFingerprint,
			RedisConnectionManager manager)
		{
			Key = key;
			SanitizedKey = sanitizedKey;
			Configuration = configuration;
			ConfigurationFingerprint = configurationFingerprint;
			this.manager = manager;
			State = RedisConnectionState.Connecting;
			LastUsedAt = DateTimeOffset.UtcNow;
		}

		public IDisposable TrackOperation() => new OperationScope(this);

		internal void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
		{
			LastDisconnectedAt = DateTimeOffset.UtcNow;
			LastFailure = e.Exception?.Message ?? e.FailureType.ToString();
			State = RedisConnectionState.Disconnected;
			Interlocked.Increment(ref manager.failureCount);

			manager.logger.LogWarning(
				e.Exception,
				"Redis connection lost for server {ServerKey}. Endpoint: {Endpoint}, FailureType: {FailureType}",
				SanitizedKey,
				e.EndPoint,
				e.FailureType);
		}

		internal void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
		{
			LastConnectedAt = DateTimeOffset.UtcNow;
			State = RedisConnectionState.Connected;
			Interlocked.Increment(ref manager.restorationCount);

			manager.logger.LogInformation(
				"Redis connection restored for server {ServerKey}. Endpoint: {Endpoint}",
				SanitizedKey,
				e.EndPoint);
		}

		internal void OnErrorMessage(object? sender, RedisErrorEventArgs e)
		{
			manager.logger.LogError(
				"Redis error for server {ServerKey}: {Message}. Endpoint: {Endpoint}",
				SanitizedKey,
				e.Message,
				e.EndPoint);
		}

		internal async ValueTask DisposeMultiplexerAsync()
		{
			if (Interlocked.CompareExchange(ref disposalState, 1, 0) != 0)
			{
				return;
			}

			ConnectionMultiplexer? mux = Multiplexer;
			Multiplexer = null;

			if (mux != null)
			{
				mux.ConnectionFailed -= OnConnectionFailed;
				mux.ConnectionRestored -= OnConnectionRestored;
				mux.ErrorMessage -= OnErrorMessage;

				try
				{
					await mux.CloseAsync(allowCommandsToComplete: true);
					await mux.DisposeAsync();
				}
				catch (Exception ex)
				{
					manager.logger.LogDebug(ex, "Error disposing multiplexer for server {ServerKey}", SanitizedKey);
				}
			}
		}

		internal void DisposeMultiplexerSync()
		{
			if (Interlocked.CompareExchange(ref disposalState, 1, 0) != 0)
			{
				return;
			}

			ConnectionMultiplexer? mux = Multiplexer;
			Multiplexer = null;

			if (mux != null)
			{
				mux.ConnectionFailed -= OnConnectionFailed;
				mux.ConnectionRestored -= OnConnectionRestored;
				mux.ErrorMessage -= OnErrorMessage;

				try
				{
					mux.Close(allowCommandsToComplete: true);
					mux.Dispose();
				}
				catch (Exception ex)
				{
					manager.logger.LogDebug(ex, "Error disposing multiplexer for server {ServerKey}", SanitizedKey);
				}
			}
		}

		internal async ValueTask CloseAndDisposeMultiplexerInternalAsync(ConnectionMultiplexer mux)
		{
			mux.ConnectionFailed -= OnConnectionFailed;
			mux.ConnectionRestored -= OnConnectionRestored;
			mux.ErrorMessage -= OnErrorMessage;

			try
			{
				await mux.CloseAsync(allowCommandsToComplete: false);
				await mux.DisposeAsync();
			}
			catch (Exception ex)
			{
				manager.logger.LogDebug(ex, "Error disposing multiplexer for server {ServerKey}", SanitizedKey);
			}
		}

		private sealed class OperationScope : IDisposable
		{
			private readonly ConnectionEntry entry;
			private int disposed;

			public OperationScope(ConnectionEntry entry)
			{
				this.entry = entry;
				Interlocked.Increment(ref this.entry.ActiveOperations);
			}

			public void Dispose()
			{
				if (Interlocked.CompareExchange(ref disposed, 1, 0) == 0)
				{
					Interlocked.Decrement(ref entry.ActiveOperations);
				}
			}
		}
	}
}

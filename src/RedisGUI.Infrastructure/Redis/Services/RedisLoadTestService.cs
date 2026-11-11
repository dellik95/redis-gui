using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Redis.LoadTesting;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.Services;

/// <summary>
/// Service dedicated to synthetic Redis load testing, benchmarking, real-time metrics streaming, and guaranteed data cleanup.
/// Uses Strategy pattern for operations and Builder pattern for result construction.
/// </summary>
public class RedisLoadTestService(
	IRedisConnectionManager connectionManager,
	ILogger<RedisLoadTestService> logger) : IRedisLoadTestService
{
	/// <inheritdoc />
	public Task<Result<RedisLoadTestResult>> RunLoadTestAsync(
		RedisConnection connection,
		RedisLoadTestRequest request,
		CancellationToken cancellationToken = default) =>
		RunStreamingLoadTestAsync(connection, request, null, cancellationToken);

	/// <inheritdoc />
	public async Task<Result<RedisLoadTestResult>> RunStreamingLoadTestAsync(
		RedisConnection connection,
		RedisLoadTestRequest request,
		Func<RedisLoadTestProgress, Task>? onProgress = null,
		CancellationToken cancellationToken = default)
	{
		if (connection.IsReadOnly && !string.Equals(request.OperationType, "read", StringComparison.OrdinalIgnoreCase))
		{
			return Result.Failure<RedisLoadTestResult>(DomainErrors.Connection.ReadOnlyMode);
		}

		Result<ConnectionMultiplexer> multiplexerResult = await connectionManager.GetConnection(connection.BuildConnectionString(), cancellationToken);
		if (multiplexerResult.IsFailure)
		{
			return Result.Failure<RedisLoadTestResult>(multiplexerResult.Error);
		}

		ConnectionMultiplexer multiplexer = multiplexerResult.Value;
		int targetDb = request.Database ?? connection.DatabaseNumber.Value;
		IDatabase db = multiplexer.GetDatabase(targetDb);

		int concurrency = Math.Clamp(request.Concurrency, 1, 50);
		int payloadSize = Math.Clamp(request.PayloadSizeBytes, 16, 65536);
		string prefix = string.IsNullOrWhiteSpace(request.KeyPrefix)
			? $"__bench_{Guid.NewGuid():N}:"
			: (request.KeyPrefix.EndsWith(':') ? request.KeyPrefix : request.KeyPrefix + ":");

		string opType = (request.OperationType ?? "mixed").ToLowerInvariant();
		string samplePayload = new string('x', payloadSize);

		const int prePopulatedKeysCount = 25;
		if (opType is "read" or "mixed")
		{
			for (int i = 0; i < prePopulatedKeysCount; i++)
			{
				await db.StringSetAsync($"{prefix}warm:{i}", samplePayload, TimeSpan.FromMinutes(10));
			}
		}

		var operationStrategy = LoadTestOperationFactory.GetOperation(opType);
		var latencies = new ConcurrentBag<double>();
		int successfulOps = 0;
		int failedOps = 0;
		int opCounter = 0;

		bool isDurationMode = request.DurationSeconds > 0;
		double maxDurationSec = isDurationMode ? Math.Clamp(request.DurationSeconds, 1, 600) : 0;
		int targetTotalOps = isDurationMode ? 0 : Math.Clamp(request.OperationsCount, 10, 100000);

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var token = cts.Token;

			var workerTasks = new Task[concurrency];
			for (int w = 0; w < concurrency; w++)
			{
				workerTasks[w] = Task.Run(async () =>
				{
					while (!token.IsCancellationRequested)
					{
						if (isDurationMode)
						{
							if (stopwatch.Elapsed.TotalSeconds >= maxDurationSec) break;
						}
						else
						{
							if (Volatile.Read(ref opCounter) >= targetTotalOps) break;
						}

						int opIndex = Interlocked.Increment(ref opCounter);
						if (!isDurationMode && opIndex > targetTotalOps) break;

						long start = System.Diagnostics.Stopwatch.GetTimestamp();
						bool ok;

						try
						{
							ok = await operationStrategy.ExecuteAsync(db, prefix, opIndex, samplePayload, prePopulatedKeysCount);
						}
						catch
						{
							ok = false;
						}

						long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start;
						double latencyMs = (double)elapsed / System.Diagnostics.Stopwatch.Frequency * 1000.0;
						latencies.Add(latencyMs);

						if (ok) Interlocked.Increment(ref successfulOps);
						else Interlocked.Increment(ref failedOps);
					}
				}, token);
			}

			if (onProgress != null)
			{
				int lastReportedCompleted = 0;
				long lastTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

				while (!Task.WhenAll(workerTasks).IsCompleted && !token.IsCancellationRequested)
				{
					if (isDurationMode && stopwatch.Elapsed.TotalSeconds >= maxDurationSec) break;

					await Task.Delay(250, CancellationToken.None).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

					int currentCompleted = successfulOps + failedOps;
					long now = System.Diagnostics.Stopwatch.GetTimestamp();
					double deltaSec = (double)(now - lastTimestamp) / System.Diagnostics.Stopwatch.Frequency;
					double liveOps = deltaSec > 0 ? (currentCompleted - lastReportedCompleted) / deltaSec : 0;
					lastReportedCompleted = currentCompleted;
					lastTimestamp = now;

					double overallOps = stopwatch.Elapsed.TotalSeconds > 0 ? currentCompleted / stopwatch.Elapsed.TotalSeconds : 0;
					double currentAvgLat = latencies.Count > 0 ? latencies.Average() : 0;
					double succRate = currentCompleted > 0 ? (double)successfulOps / currentCompleted * 100.0 : 100.0;

					try
					{
						await onProgress(new RedisLoadTestProgress
						{
							ElapsedSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 2),
							TotalDurationSeconds = maxDurationSec,
							CompletedOperations = currentCompleted,
							TotalOperations = targetTotalOps,
							CurrentOpsPerSecond = Math.Round(liveOps, 1),
							OverallOpsPerSecond = Math.Round(overallOps, 1),
							AvgLatencyMs = Math.Round(currentAvgLat, 3),
							SuccessRatePercent = Math.Round(succRate, 1),
							InProgress = true
						});
					}
					catch
					{
						// Ignore write errors to client during streaming
					}
				}
			}

			await Task.WhenAll(workerTasks);
		}
		finally
		{
			stopwatch.Stop();
		}

		int cleanedCount = await CleanupBenchmarkKeysAsync(multiplexer, targetDb, prefix);

		var result = new BenchmarkResultBuilder()
			.WithCounters(successfulOps, failedOps)
			.WithDuration(stopwatch.Elapsed.TotalMilliseconds)
			.WithLatencies(latencies)
			.WithCleanedKeys(cleanedCount)
			.WithMetadata(opType, concurrency)
			.Build();

		return Result.Success(result);
	}

	private async Task<int> CleanupBenchmarkKeysAsync(ConnectionMultiplexer multiplexer, int targetDb, string prefix)
	{
		int cleanedCount = 0;
		try
		{
			var servers = multiplexer.GetServers().Where(s => s is { IsConnected: true, Features.Scan: true });
			var keysToDelete = new List<RedisKey>();
			var db = multiplexer.GetDatabase(targetDb);

			foreach (var server in servers)
			{
				await foreach (var k in server.KeysAsync(targetDb, pattern: $"{prefix}*"))
				{
					keysToDelete.Add(k);
				}
			}

			if (keysToDelete.Count > 0)
			{
				const int batchSize = 1000;
				for (int i = 0; i < keysToDelete.Count; i += batchSize)
				{
					var chunk = keysToDelete.Skip(i).Take(batchSize).ToArray();
					cleanedCount += (int)await db.KeyDeleteAsync(chunk);
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to clean up benchmark keys with prefix {Prefix}", prefix);
		}
		return cleanedCount;
	}
}

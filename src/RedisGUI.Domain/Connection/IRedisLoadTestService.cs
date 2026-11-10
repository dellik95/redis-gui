using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service responsible for executing Redis synthetic load testing and benchmarks.
/// </summary>
public interface IRedisLoadTestService
{
	/// <summary>
	/// Executes synthetic load testing / benchmark against Redis with automatic cleanup of generated test keys.
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="request">Load test parameters (operations, concurrency, payload size, database, etc.).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Comprehensive benchmark results with latency percentiles and throughput.</returns>
	Task<Result<RedisLoadTestResult>> RunLoadTestAsync(
		RedisConnection connection,
		RedisLoadTestRequest request,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Executes synthetic load testing with duration support, real-time progress callbacks, and live cancellation.
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="request">Load test parameters.</param>
	/// <param name="onProgress">Optional callback invoked with live metrics during benchmark execution.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Comprehensive benchmark results with latency percentiles and throughput.</returns>
	Task<Result<RedisLoadTestResult>> RunStreamingLoadTestAsync(
		RedisConnection connection,
		RedisLoadTestRequest request,
		Func<RedisLoadTestProgress, Task>? onProgress = null,
		CancellationToken cancellationToken = default);
}

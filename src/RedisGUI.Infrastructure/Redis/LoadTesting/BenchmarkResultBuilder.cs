using RedisGUI.Domain.Redis;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.LoadTesting;

/// <summary>
/// Builder for calculating benchmark metrics, latency percentiles, and constructing RedisLoadTestResult.
/// </summary>
public sealed class BenchmarkResultBuilder
{
	private int _successfulOps;
	private int _failedOps;
	private double _totalMs;
	private int _cleanedKeys;
	private string _opType = "mixed";
	private int _concurrency = 1;
	private readonly List<double> _latencies = [];

	public BenchmarkResultBuilder WithCounters(int successful, int failed)
	{
		_successfulOps = successful;
		_failedOps = failed;
		return this;
	}

	public BenchmarkResultBuilder WithDuration(double totalMs)
	{
		_totalMs = Math.Max(0.001, totalMs);
		return this;
	}

	public BenchmarkResultBuilder WithLatencies(IEnumerable<double> latencies)
	{
		_latencies.Clear();
		_latencies.AddRange(latencies);
		return this;
	}

	public BenchmarkResultBuilder WithCleanedKeys(int count)
	{
		_cleanedKeys = count;
		return this;
	}

	public BenchmarkResultBuilder WithMetadata(string opType, int concurrency)
	{
		_opType = opType;
		_concurrency = concurrency;
		return this;
	}

	public RedisLoadTestResult Build()
	{
		var sorted = _latencies.OrderBy(x => x).ToList();
		double opsPerSec = _successfulOps / (_totalMs / 1000.0);

		double minLat = sorted.Count > 0 ? sorted.First() : 0.0;
		double maxLat = sorted.Count > 0 ? sorted.Last() : 0.0;
		double avgLat = sorted.Count > 0 ? sorted.Average() : 0.0;

		double GetPercentile(double percentile)
		{
			if (sorted.Count == 0) return 0.0;
			int idx = (int)Math.Ceiling(percentile * sorted.Count) - 1;
			idx = Math.Clamp(idx, 0, sorted.Count - 1);
			return sorted[idx];
		}

		return new RedisLoadTestResult
		{
			TotalOperations = _successfulOps + _failedOps,
			SuccessfulOperations = _successfulOps,
			FailedOperations = _failedOps,
			TotalDurationMs = Math.Round(_totalMs, 2),
			OpsPerSecond = Math.Round(opsPerSec, 1),
			AvgLatencyMs = Math.Round(avgLat, 3),
			MinLatencyMs = Math.Round(minLat, 3),
			MaxLatencyMs = Math.Round(maxLat, 3),
			P50LatencyMs = Math.Round(GetPercentile(0.50), 3),
			P90LatencyMs = Math.Round(GetPercentile(0.90), 3),
			P95LatencyMs = Math.Round(GetPercentile(0.95), 3),
			P99LatencyMs = Math.Round(GetPercentile(0.99), 3),
			CleanedKeysCount = _cleanedKeys,
			OperationType = _opType,
			Concurrency = _concurrency,
			ExecutedAt = DateTime.UtcNow
		};
	}
}

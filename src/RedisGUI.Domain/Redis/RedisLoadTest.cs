using System;

namespace RedisGUI.Domain.Redis;

/// <summary>
/// Configuration parameters for running synthetic Redis load test
/// </summary>
public sealed record RedisLoadTestRequest(
	int OperationsCount = 1000,
	int Concurrency = 5,
	string OperationType = "mixed",
	int PayloadSizeBytes = 128,
	int? Database = null,
	string? KeyPrefix = null,
	int DurationSeconds = 0);

/// <summary>
/// Real-time streaming progress metrics emitted during a running load test
/// </summary>
public sealed class RedisLoadTestProgress
{
	public double ElapsedSeconds { get; set; }
	public double TotalDurationSeconds { get; set; }
	public int CompletedOperations { get; set; }
	public int TotalOperations { get; set; }
	public double CurrentOpsPerSecond { get; set; }
	public double OverallOpsPerSecond { get; set; }
	public double AvgLatencyMs { get; set; }
	public double SuccessRatePercent { get; set; }
	public bool InProgress { get; set; } = true;
}

/// <summary>
/// Execution metrics and latency distribution of a Redis load test
/// </summary>
public sealed class RedisLoadTestResult
{
	public int TotalOperations { get; set; }
	public int SuccessfulOperations { get; set; }
	public int FailedOperations { get; set; }
	public double TotalDurationMs { get; set; }
	public double OpsPerSecond { get; set; }
	public double AvgLatencyMs { get; set; }
	public double MinLatencyMs { get; set; }
	public double MaxLatencyMs { get; set; }
	public double P50LatencyMs { get; set; }
	public double P90LatencyMs { get; set; }
	public double P95LatencyMs { get; set; }
	public double P99LatencyMs { get; set; }
	public int CleanedKeysCount { get; set; }
	public string OperationType { get; set; } = "mixed";
	public int Concurrency { get; set; } = 5;
	public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

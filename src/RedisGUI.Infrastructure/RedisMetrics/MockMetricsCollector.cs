using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.RedisMetrics.Abstractions;
using RedisGUI.Infrastructure.Configuration;
using System.Collections.Concurrent;
using DomainRedisMetrics = RedisGUI.Domain.RedisMetrics.RedisMetrics;

namespace RedisGUI.Infrastructure.RedisMetrics;

/// <summary>
/// Mock implementation of IMetricsCollector that generates dummy Redis metrics data.
/// </summary>
public sealed class MockMetricsCollector : IMetricsCollector
{
	private readonly ConcurrentDictionary<Guid, DomainRedisMetrics> metricsCache = [];
	private readonly IOptions<RedisMockOptions> mockOptions;
	private readonly ILogger<MockMetricsCollector>? logger;
	private readonly Random random = new();

	/// <summary>
	/// Initializes a new instance of MockMetricsCollector
	/// </summary>
	public MockMetricsCollector(
		IOptions<RedisMockOptions> mockOptions,
		ILogger<MockMetricsCollector>? logger = null)
	{
		this.mockOptions = mockOptions;
		this.logger = logger;
	}

	/// <summary>
	/// Collects mock metrics for a specified Redis connection
	/// </summary>
	/// <param name="connection">The Redis connection to collect metrics from</param>
	/// <param name="cancellationToken">Token to cancel the operation</param>
	/// <returns>A Result containing the collected Redis metrics or an error</returns>
	public Task<Result<DomainRedisMetrics>> CollectMetrics(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		try
		{
			// Get or create metrics for this connection
			DomainRedisMetrics metrics = metricsCache.GetOrAdd(connection.Id, _ => new DomainRedisMetrics());

			// Update metrics with realistic-looking dummy data
			DomainRedisMetrics previousMetrics = metrics;
			metrics = new DomainRedisMetrics
			{
				// Network metrics - simulate realistic values
				NetworkSpeedIn = Math.Max(0, previousMetrics.NetworkSpeedIn + random.NextDouble() * 10 - 5),
				NetworkSpeedOut = Math.Max(0, previousMetrics.NetworkSpeedOut + random.NextDouble() * 10 - 5),

				// Key count - simulate based on connection (can be configured)
				KeyCount = mockOptions.Value.SeedDummyData
					? mockOptions.Value.DummyDataKeyCount + random.Next(-10, 10)
					: random.Next(0, 1000),

				// Memory usage - simulate realistic memory consumption
				MemoryUsage = Math.Max(1024 * 1024, previousMetrics.MemoryUsage + random.Next(-1024 * 1024, 1024 * 1024)),

				// CPU usage - simulate realistic CPU percentages
				CpuUsageSystem = Math.Max(0, Math.Min(100, previousMetrics.CpuUsageSystem + random.NextDouble() * 2 - 1)),
				CpuUsageUser = Math.Max(0, Math.Min(100, previousMetrics.CpuUsageUser + random.NextDouble() * 2 - 1)),
				CpuUsageInPercent = Math.Max(0, Math.Min(100, previousMetrics.CpuUsageInPercent + random.NextDouble() * 2 - 1)),

				// Uptime - increment by polling interval
				UptimeInSeconds = previousMetrics.UptimeInSeconds > 0
					? previousMetrics.UptimeInSeconds + 5
					: random.Next(3600, 86400 * 7) // Random uptime between 1 hour and 7 days
			};

			// Update cache
			metricsCache.AddOrUpdate(connection.Id, metrics, (_, _) => metrics);

			logger?.LogDebug("Collected mock metrics for connection {ConnectionId}", connection.Id);
			return Task.FromResult(Result.Success(metrics));
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error collecting mock metrics. Message: {Message}", ex.Message);
			return Task.FromResult(Result.Failure<DomainRedisMetrics>(DomainErrors.MetricsCollection.Exception));
		}
	}
}


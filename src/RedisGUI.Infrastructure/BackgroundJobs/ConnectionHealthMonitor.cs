using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RedisGUI.Domain.Connection;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RedisGUI.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that monitors Redis connection health
/// </summary>
public sealed class ConnectionHealthMonitor : BackgroundService, IConnectionHealthMonitor
{
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<ConnectionHealthMonitor> logger;
	private readonly ConcurrentDictionary<Guid, ConnectionHealth> healthStatuses = new();
	private readonly TimeSpan checkInterval = TimeSpan.FromSeconds(30);
	private readonly int failureThreshold = 3;
	private readonly double degradedThresholdMs = 1000;

	public ConnectionHealthMonitor(
		IServiceProvider serviceProvider,
		ILogger<ConnectionHealthMonitor> logger)
	{
		this.serviceProvider = serviceProvider;
		this.logger = logger;
	}

	/// <summary>
	/// Get the current health status for a connection
	/// </summary>
	public ConnectionHealth? GetHealth(Guid connectionId)
	{
		healthStatuses.TryGetValue(connectionId, out var health);
		return health;
	}

	/// <summary>
	/// Get all connection health statuses
	/// </summary>
	public IEnumerable<ConnectionHealth> GetAllHealth()
	{
		return healthStatuses.Values;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Connection Health Monitor started");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await CheckAllConnectionsHealth(stoppingToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error checking connection health");
			}

			await Task.Delay(checkInterval, stoppingToken);
		}

		logger.LogInformation("Connection Health Monitor stopped");
	}

	private async Task CheckAllConnectionsHealth(CancellationToken cancellationToken)
	{
		using var scope = serviceProvider.CreateScope();
		var connectionRepository = scope.ServiceProvider.GetRequiredService<IRedisConnectionRepository>();
		var connectionService = scope.ServiceProvider.GetRequiredService<IConnectionService>();

		var connectionsResult = await connectionRepository.GetAllConnectionsAsync(cancellationToken);

		if (connectionsResult.IsFailure)
		{
			logger.LogWarning("Failed to retrieve connections for health check");
			return;
		}

		var connections = connectionsResult.Value;

		foreach (var connection in connections)
		{
			await CheckConnectionHealth(connection, connectionService, cancellationToken);
		}
	}

	private async Task CheckConnectionHealth(
		RedisConnection connection,
		IConnectionService connectionService,
		CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		try
		{
			var result = await connectionService.CheckConnection(connection);
			sw.Stop();

			var responseTimeMs = sw.Elapsed.TotalMilliseconds;

			if (result.IsSuccess)
			{
				ConnectionHealth health;

				if (responseTimeMs > degradedThresholdMs)
				{
					health = ConnectionHealth.Degraded(
						connection.Id,
						responseTimeMs,
						$"Slow response time: {responseTimeMs:F0}ms");
					
					logger.LogWarning(
						"Connection {ConnectionId} is degraded (response time: {ResponseTime}ms)",
						connection.Id,
						responseTimeMs);
				}
				else
				{
					health = ConnectionHealth.Healthy(connection.Id, responseTimeMs);
					logger.LogDebug(
						"Connection {ConnectionId} is healthy (response time: {ResponseTime}ms)",
						connection.Id,
						responseTimeMs);
				}

				healthStatuses.AddOrUpdate(connection.Id, health, (_, _) => health);
			}
			else
			{
				var previousFailures = healthStatuses.TryGetValue(connection.Id, out var prev)
					? prev.ConsecutiveFailures + 1
					: 1;

				var health = ConnectionHealth.Unhealthy(
					connection.Id,
					result.Error.Message,
					previousFailures);

				healthStatuses.AddOrUpdate(connection.Id, health, (_, _) => health);

				if (previousFailures >= failureThreshold)
				{
					logger.LogError(
						"Connection {ConnectionId} has failed {Failures} consecutive times: {Error}",
						connection.Id,
						previousFailures,
						result.Error.Message);
				}
			}
		}
		catch (Exception ex)
		{
			sw.Stop();

			var previousFailures = healthStatuses.TryGetValue(connection.Id, out var prev)
				? prev.ConsecutiveFailures + 1
				: 1;

			var health = ConnectionHealth.Unhealthy(
				connection.Id,
				ex.Message,
				previousFailures);

			healthStatuses.AddOrUpdate(connection.Id, health, (_, _) => health);

			logger.LogError(ex,
				"Exception checking connection {ConnectionId} health",
				connection.Id);
		}
	}
}

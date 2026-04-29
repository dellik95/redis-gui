using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace RedisGUI.Infrastructure.HealthChecks;

/// <summary>
/// Health check for background services
/// </summary>
public class BackgroundServicesHealthCheck : IHealthCheck
{
	private readonly IServiceProvider serviceProvider;

	public BackgroundServicesHealthCheck(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
	}

	public Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			// Check if the connection health monitor is accessible
			var healthMonitor = serviceProvider.GetService<IConnectionHealthMonitor>();
			
			if (healthMonitor == null)
			{
				return Task.FromResult(
					HealthCheckResult.Degraded("Connection Health Monitor not available"));
			}

			// Get health statuses
			var allHealth = healthMonitor.GetAllHealth();
			var healthList = allHealth.ToList();

			var data = new Dictionary<string, object>
			{
				{ "monitored_connections", healthList.Count },
				{ "healthy_connections", healthList.Count(h => h.Status == ConnectionHealthStatus.Healthy) },
				{ "degraded_connections", healthList.Count(h => h.Status == ConnectionHealthStatus.Degraded) },
				{ "unhealthy_connections", healthList.Count(h => h.Status == ConnectionHealthStatus.Unhealthy) }
			};

			return Task.FromResult(
				HealthCheckResult.Healthy("Background services are running", data));
		}
		catch (Exception ex)
		{
			return Task.FromResult(
				HealthCheckResult.Unhealthy("Background services check failed", ex));
		}
	}
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedisGUI.Infrastructure.Persistence;

namespace RedisGUI.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies database connectivity via EF Core.
/// </summary>
public class DatabaseHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{

	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
			if (!canConnect)
			{
				return HealthCheckResult.Unhealthy("Database unreachable.");
			}

			// Perform a simple query to verify database is responsive
			int testQuery = await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

			return HealthCheckResult.Healthy(
				"Database is reachable and responsive.",
				new Dictionary<string, object>
				{
					["Database"] = dbContext.Database.GetDbConnection().Database ?? "Unknown",
					["Provider"] = dbContext.Database.ProviderName ?? "Unknown"
				});
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy(
				"Database health check failed.",
				ex,
				new Dictionary<string, object>
				{
					["Error"] = ex.Message,
					["ErrorType"] = ex.GetType().Name
				});
		}
	}
}


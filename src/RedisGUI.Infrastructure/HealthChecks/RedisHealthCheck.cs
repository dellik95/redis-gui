using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedisGUI.Infrastructure.Redis;
using StackExchange.Redis;

namespace RedisGUI.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies Redis connectivity via the connection pool.
/// </summary>
public class RedisHealthCheck(
	IConnectionPool connectionPool,
	IOptions<RedisHealthCheckOptions> options)
	: IHealthCheck
{
	private readonly RedisHealthCheckOptions options = options.Value;

	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(options.ConnectionString))
		{
			return HealthCheckResult.Healthy("Redis health check disabled (no connection string configured).");
		}

		try
		{
			Result<ConnectionMultiplexer> connectionResult = await connectionPool.GetConnection(options.ConnectionString, cancellationToken);

			if (connectionResult.IsFailure)
			{
				return HealthCheckResult.Unhealthy(
					"Failed to acquire Redis connection.",
					data: new Dictionary<string, object>
					{
						["Error"] = connectionResult.Error.Message,
						["ErrorCode"] = connectionResult.Error.Code
					});
			}

			IDatabase database = connectionResult.Value.GetDatabase(options.Database);
			TimeSpan ping = await database.PingAsync();

			if (ping <= TimeSpan.Zero)
			{
				return HealthCheckResult.Unhealthy("Redis ping failed.");
			}

			return HealthCheckResult.Healthy(
				$"Redis reachable. Ping: {ping.TotalMilliseconds:F2}ms",
				new Dictionary<string, object>
				{
					["Ping"] = ping.TotalMilliseconds,
					["Database"] = options.Database
				});
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy(
				"Redis health check failed.",
				ex,
				new Dictionary<string, object>
				{
					["Error"] = ex.Message,
					["ErrorType"] = ex.GetType().Name
				});
		}
	}
}


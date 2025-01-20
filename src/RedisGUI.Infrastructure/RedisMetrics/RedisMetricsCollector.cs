using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using RedisGUI.Domain.RedisMetrics.Abstractions;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Extensions;
using RedisGUI.Infrastructure.Redis;

namespace RedisGUI.Infrastructure.RedisMetrics;

public class RedisMetricsCollector : IMetricsCollector
{
	private readonly IConnectionPool connectionPool;
	private readonly ConcurrentDictionary<Guid, Domain.RedisMetrics.RedisMetrics> metricsMap = [];
	private readonly IOptions<MetricsCollectorOptions> options;
	private readonly ILogger<RedisMetricsCollector> logger;

	public RedisMetricsCollector(IServiceProvider serviceProvider, IOptions<MetricsCollectorOptions> options, ILogger<RedisMetricsCollector> logger)
	{
		var scope = serviceProvider.CreateScope();
		this.connectionPool = scope.ServiceProvider.GetRequiredService<IConnectionPool>();
		scope.ServiceProvider.GetRequiredService<IRedisConnectionRepository>();
		this.options = options;
		this.logger = logger;
	}

	public async Task<Result<Domain.RedisMetrics.RedisMetrics>> CollectMetrics(RedisConnection connection, CancellationToken cancellationToken = default)
	{
		try
		{
			var multiplexerResult = await connectionPool.GetConnection(connection.BuildConnectionString(), cancellationToken);
			if (multiplexerResult.IsFailure)
			{
				return Result.Failure<Domain.RedisMetrics.RedisMetrics>(multiplexerResult.Error);
			}

			var multiplexer = multiplexerResult.Value;
			var server = multiplexer.GetServer(connection.ServerHost.Value, connection.ServerPort.Value);

			var info = await server.InfoAsync();
			var infoSections = info.ToDictionary(x => x.Key, x => x.Select(pair => new Tuple<string, string>(pair.Key, pair.Value)));

			var newMetrics = metricsMap.AddOrUpdate(connection.Id, _ => new Domain.RedisMetrics.RedisMetrics(),
				(_, redisMetrics) =>
				{
					var currentMetrics = new Domain.RedisMetrics.RedisMetrics();

					currentMetrics
						.PopulateMetrics(infoSections)
						.CalculateMetrics(redisMetrics, this.options.Value.PoolingIntervalInSec);

					return currentMetrics;
				});

			return newMetrics;
		}
		catch (Exception ex)
		{
			this.logger.LogError(ex, "Error occured on processing metrics. Message: {Message}", ex.Message);
			return Result.Failure<Domain.RedisMetrics.RedisMetrics>(new Error("MetricsCollection.Exception", ex.Message));
		}
	}
}

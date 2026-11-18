using System;
using RedisGUI.Application.Core.Services;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.RedisMetrics.Abstractions;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.RedisMetrics.Queries;

public sealed record GetRedisMetricsQuery(Guid ConnectionId) : IQuery<Domain.RedisMetrics.RedisMetrics>;

internal sealed class GetRedisMetricsQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IMetricsCollector metricsCollector,
	IConnectionAccessService? access = null) : IQueryHandler<GetRedisMetricsQuery, Domain.RedisMetrics.RedisMetrics>
{
	public async Task<Result<Domain.RedisMetrics.RedisMetrics>> Handle(GetRedisMetricsQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<Domain.RedisMetrics.RedisMetrics>(connectionResult.Error);
		}

		var metricsResult = await metricsCollector.CollectMetrics(connectionResult.Value, cancellationToken);

		return metricsResult.IsFailure ? Result.Failure<Domain.RedisMetrics.RedisMetrics>(metricsResult.Error) : metricsResult.Value;
	}
}

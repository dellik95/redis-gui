using FluentResults;
using System;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
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
		if (connectionResult.IsFailed)
		{
			return Result.Fail<Domain.RedisMetrics.RedisMetrics>(connectionResult.Errors);
		}

		var metricsResult = await metricsCollector.CollectMetrics(connectionResult.Value, cancellationToken);

		return metricsResult.IsFailed ? Result.Fail<Domain.RedisMetrics.RedisMetrics>(metricsResult.Errors) : metricsResult.Value;
	}
}

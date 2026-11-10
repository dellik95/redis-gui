using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.RedisMetrics.Queries
{
	public record GetRedisMetricsQuery(Guid ConnectionId) : IQuery<Domain.RedisMetrics.RedisMetrics>;
}

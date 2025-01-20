using System;
using RedisGUI.Application.Abstraction.Messaging;

namespace RedisGUI.Application.RedisMetrics.Queries
{
	public record GetRedisMetricsQuery(Guid ConnectionId) : IQuery<Domain.RedisMetrics.RedisMetrics>;
}

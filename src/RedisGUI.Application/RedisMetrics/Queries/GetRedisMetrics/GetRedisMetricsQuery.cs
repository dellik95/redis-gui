using System;
using RedisGUI.Application.Abstraction.Messaging;

namespace RedisGUI.Application.RedisMetrics.Queries.GetRedisMetrics
{
	public record GetRedisMetricsQuery(Guid ConnectionId) : IQuery<GetRedisMetricsResponse>;
}

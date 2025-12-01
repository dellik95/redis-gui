using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.RedisMetrics.Abstractions;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.RedisMetrics.Queries
{
	internal class GetRedisMetricsQueryHandler : IQueryHandler<GetRedisMetricsQuery, Domain.RedisMetrics.RedisMetrics>
	{
		private readonly IRedisConnectionRepository connectionRepository;
		private readonly IMetricsCollector metricsCollector;

		public GetRedisMetricsQueryHandler(IRedisConnectionRepository connectionRepository, IMetricsCollector metricsCollector)
		{
			this.metricsCollector = metricsCollector;
			this.connectionRepository = connectionRepository;
		}

		public async Task<Result<Domain.RedisMetrics.RedisMetrics>> Handle(GetRedisMetricsQuery request, CancellationToken cancellationToken)
		{
			var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);

			if (connectionResult.IsFailure)
			{
				return Result.Failure<Domain.RedisMetrics.RedisMetrics>(connectionResult.Error);
			}

			var metricsResult = await metricsCollector.CollectMetrics(connectionResult.Value, cancellationToken);

			return metricsResult.IsFailure ? Result.Failure<Domain.RedisMetrics.RedisMetrics>(metricsResult.Error) : metricsResult.Value;
		}
	}
}

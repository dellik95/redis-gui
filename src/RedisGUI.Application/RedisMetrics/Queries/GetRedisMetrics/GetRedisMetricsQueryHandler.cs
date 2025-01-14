using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.RedisMetrics.Queries.GetRedisMetrics
{
	public class GetRedisMetricsQueryHandler : IQueryHandler<GetRedisMetricsQuery, GetRedisMetricsResponse>
	{
		private readonly IRedisConnectionRepository connectionRepository;
		private readonly IConnectionService connectionService;

		/// <summary>
		/// Initializes a new instance of the GetRedisMetricsQueryHandler.
		/// </summary>
		/// <param name="connectionRepository">Repository for Redis connections.</param>
		/// <param name="connectionService">Service for Redis connection operations.</param>
		public GetRedisMetricsQueryHandler(
			IRedisConnectionRepository connectionRepository,
			IConnectionService connectionService)
		{
			this.connectionRepository = connectionRepository;
			this.connectionService = connectionService;
		}

		public async Task<Result<GetRedisMetricsResponse>> Handle(GetRedisMetricsQuery request, CancellationToken cancellationToken)
		{
			var connectionResult = await connectionRepository.GetConnectionByIdAsync(
				request.ConnectionId,
				cancellationToken);

			if (connectionResult.IsFailure)
			{
				return Result.Failure<GetRedisMetricsResponse>(connectionResult.Error);
			}

			var metrics = await connectionService.GetMetrics(connectionResult);

			if (metrics.IsFailure)
			{
				return Result.Failure<GetRedisMetricsResponse>(metrics.Error);
			}

			var metricsValue = metrics.Value;
			var response = new GetRedisMetricsResponse(metricsValue.Cpu, metricsValue.Memory, metricsValue.ConnectedClients, metricsValue.NetworkSpeed, metricsValue.KeyCount);

			return Result.Success(response);
		}
	}
}

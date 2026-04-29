using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Queries.SubscribeToChannel;

/// <summary>
/// Handler for subscribing to Redis Pub/Sub channels.
/// </summary>
public sealed class SubscribeToChannelQueryHandler : IQueryHandler<SubscribeToChannelQuery, SubscribeToChannelResponse>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisPubSubService pubSubService;

	public SubscribeToChannelQueryHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisPubSubService pubSubService)
	{
		this.connectionRepository = connectionRepository;
		this.pubSubService = pubSubService;
	}

	public async Task<Result<SubscribeToChannelResponse>> Handle(SubscribeToChannelQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		
		if (connectionResult.IsFailure)
		{
			return Result.Failure<SubscribeToChannelResponse>(connectionResult.Error);
		}

		var result = await pubSubService.SubscribeToChannelAsync(
			connectionResult.Value,
			request.Channel,
			cancellationToken);

		if (result.IsFailure)
		{
			return Result.Failure<SubscribeToChannelResponse>(result.Error);
		}

		var subscriptionId = result.Value;
		var response = new SubscribeToChannelResponse(subscriptionId, request.Channel, true);

		return Result.Success(response);
	}
}

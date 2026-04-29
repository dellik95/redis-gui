using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.UnsubscribeFromChannel;

/// <summary>
/// Handler for unsubscribing from Redis Pub/Sub channels.
/// </summary>
public sealed class UnsubscribeFromChannelCommandHandler : ICommandHandler<UnsubscribeFromChannelCommand>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisPubSubService pubSubService;

	public UnsubscribeFromChannelCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisPubSubService pubSubService)
	{
		this.connectionRepository = connectionRepository;
		this.pubSubService = pubSubService;
	}

	public async Task<Result> Handle(UnsubscribeFromChannelCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		
		if (connectionResult.IsFailure)
		{
			return Result.Failure(connectionResult.Error);
		}

		return await pubSubService.UnsubscribeFromChannelAsync(
			connectionResult.Value,
			request.SubscriptionId,
			cancellationToken);
	}
}

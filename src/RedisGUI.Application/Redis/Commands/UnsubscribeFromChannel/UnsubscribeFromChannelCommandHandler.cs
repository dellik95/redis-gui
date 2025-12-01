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
	private readonly IConnectionService connectionService;

	public UnsubscribeFromChannelCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IConnectionService connectionService)
	{
		this.connectionRepository = connectionRepository;
		this.connectionService = connectionService;
	}

	public async Task<Result> Handle(UnsubscribeFromChannelCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		
		if (connectionResult.IsFailure)
		{
			return Result.Failure(connectionResult.Error);
		}

		return await connectionService.UnsubscribeFromChannel(
			connectionResult.Value,
			request.SubscriptionId,
			cancellationToken);
	}
}

using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.PublishMessage;

/// <summary>
/// Handler for publishing messages to Redis Pub/Sub channels.
/// </summary>
public sealed class PublishMessageCommandHandler : ICommandHandler<PublishMessageCommand>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IConnectionService connectionService;

	public PublishMessageCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IConnectionService connectionService)
	{
		this.connectionRepository = connectionRepository;
		this.connectionService = connectionService;
	}

	public async Task<Result> Handle(PublishMessageCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		
		if (connectionResult.IsFailure)
		{
			return Result.Failure(connectionResult.Error);
		}

		return await connectionService.PublishMessage(connectionResult.Value, request.Channel, request.Message);
	}
}

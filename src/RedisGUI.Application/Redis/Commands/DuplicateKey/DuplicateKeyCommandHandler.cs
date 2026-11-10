using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.DuplicateKey;

internal sealed class DuplicateKeyCommandHandler : ICommandHandler<DuplicateKeyCommand>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisValueService valueService;

	public DuplicateKeyCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisValueService valueService)
	{
		this.connectionRepository = connectionRepository;
		this.valueService = valueService;
	}

	public async Task<Result> Handle(DuplicateKeyCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure(connectionResult.Error);
		}

		if (connectionResult.Value.IsReadOnly)
		{
			return Result.Failure(DomainErrors.Connection.ReadOnlyMode);
		}

		return await valueService.DuplicateKeyAsync(
			connectionResult.Value,
			request.SourceKey,
			request.TargetKey,
			request.Database,
			cancellationToken);
	}
}

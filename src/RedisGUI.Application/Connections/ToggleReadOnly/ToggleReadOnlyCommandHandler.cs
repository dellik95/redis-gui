using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Connections.ToggleReadOnly;

internal sealed class ToggleReadOnlyCommandHandler : ICommandHandler<ToggleReadOnlyCommand, bool>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IUnitOfWork unitOfWork;

	public ToggleReadOnlyCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IUnitOfWork unitOfWork)
	{
		this.connectionRepository = connectionRepository;
		this.unitOfWork = unitOfWork;
	}

	public async Task<Result<bool>> Handle(ToggleReadOnlyCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<bool>(connectionResult.Error);
		}

		var connection = connectionResult.Value;
		connection.SetReadOnly(request.IsReadOnly);

		connectionRepository.Update(connection);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success(connection.IsReadOnly);
	}
}

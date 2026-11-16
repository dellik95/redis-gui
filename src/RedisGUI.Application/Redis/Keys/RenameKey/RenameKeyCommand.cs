using RedisGUI.Domain.Audit;
using RedisGUI.Shared.Mediator;
using System;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.RenameKey;

/// <summary>
/// Command to rename a key in Redis
/// </summary>
[Auditable(AuditAction.Update, "Redis")]
public sealed record RenameKeyCommand(
	Guid ConnectionId,
	string OldKey,
	string NewKey,
	bool Overwrite = false,
	int? Database = null
) : ICommand;

internal sealed class RenameKeyCommandHandler : ICommandHandler<RenameKeyCommand>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisValueService valueService;

	public RenameKeyCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisValueService valueService)
	{
		this.connectionRepository = connectionRepository;
		this.valueService = valueService;
	}

	public async Task<Result> Handle(RenameKeyCommand request, CancellationToken cancellationToken)
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

		return await valueService.RenameKeyAsync(
			connectionResult.Value,
			request.OldKey,
			request.NewKey,
			request.Overwrite,
			request.Database,
			cancellationToken);
	}
}

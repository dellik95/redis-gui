using RedisGUI.Domain.Audit;
using System;
using RedisGUI.Shared.Mediator;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.DeleteRedisKey;

/// <summary>
/// Command to delete a Redis key.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Key">The key to delete.</param>
/// <param name="Database">Optional database index.</param>
[Auditable(AuditAction.Delete, "Redis")]
public sealed record DeleteRedisKeyCommand(Guid ConnectionId, string Key, int? Database = null) : ICommand<bool>;

/// <summary>
/// Handles the command to delete a Redis key.
/// </summary>
internal sealed class DeleteRedisKeyCommandHandler : ICommandHandler<DeleteRedisKeyCommand, bool>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisKeyService keyService;

	public DeleteRedisKeyCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisKeyService keyService)
	{
		this.connectionRepository = connectionRepository;
		this.keyService = keyService;
	}

	public async Task<Result<bool>> Handle(DeleteRedisKeyCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(
			request.ConnectionId,
			cancellationToken);

		if (connectionResult.IsFailure)
		{
			return Result.Failure<bool>(connectionResult.Error);
		}

		if (connectionResult.Value.IsReadOnly)
		{
			return Result.Failure<bool>(DomainErrors.Connection.ReadOnlyMode);
		}

		return await keyService.DeleteKeyAsync(connectionResult.Value, request.Key, cancellationToken, request.Database);
	}
}

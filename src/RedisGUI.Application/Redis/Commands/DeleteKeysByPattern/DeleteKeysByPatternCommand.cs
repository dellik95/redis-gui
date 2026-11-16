using RedisGUI.Domain.Audit;
using System;
using RedisGUI.Shared.Mediator;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;

/// <summary>
/// Command to delete Redis keys matching a pattern.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Pattern">Pattern to match keys (e.g., "user:*", "session:*").</param>
/// <param name="Database">Optional database index.</param>
[Auditable(AuditAction.Delete, "Redis")]
public sealed record DeleteKeysByPatternCommand(
	Guid ConnectionId,
	string Pattern,
	int? Database = null) : ICommand<DeleteKeysByPatternResponse>;

/// <summary>
/// Handles the command to delete keys matching a pattern.
/// </summary>
internal sealed class DeleteKeysByPatternCommandHandler : ICommandHandler<DeleteKeysByPatternCommand, DeleteKeysByPatternResponse>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisKeyService keyService;

	public DeleteKeysByPatternCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisKeyService keyService)
	{
		this.connectionRepository = connectionRepository;
		this.keyService = keyService;
	}

	public async Task<Result<DeleteKeysByPatternResponse>> Handle(
		DeleteKeysByPatternCommand request,
		CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(
			request.ConnectionId,
			cancellationToken);

		if (connectionResult.IsFailure)
		{
			return Result.Failure<DeleteKeysByPatternResponse>(connectionResult.Error);
		}

		if (connectionResult.Value.IsReadOnly)
		{
			return Result.Failure<DeleteKeysByPatternResponse>(DomainErrors.Connection.ReadOnlyMode);
		}

		var deleteResult = await keyService.DeleteKeysByPatternAsync(
			connectionResult.Value,
			request.Pattern,
			cancellationToken,
			request.Database);

		if (deleteResult.IsFailure)
		{
			return Result.Failure<DeleteKeysByPatternResponse>(deleteResult.Error);
		}

		var deletedCount = deleteResult.Value;

		var response = new DeleteKeysByPatternResponse(
			deletedCount,
			request.Pattern);

		return Result.Success(response);
	}
}

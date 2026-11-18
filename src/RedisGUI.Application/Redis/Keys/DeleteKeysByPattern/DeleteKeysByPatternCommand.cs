using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
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
	int? Database = null) : ICommand<DeleteKeysByPatternResponse>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

/// <summary>
/// Handles the command to delete keys matching a pattern.
/// </summary>
internal sealed class DeleteKeysByPatternCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisKeyService keyService,
	IConnectionAccessService access) : ICommandHandler<DeleteKeysByPatternCommand, DeleteKeysByPatternResponse>
{
	public async Task<Result<DeleteKeysByPatternResponse>> Handle(
		DeleteKeysByPatternCommand request,
		CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true);

		if (connectionResult.IsFailure)
		{
			return Result.Failure<DeleteKeysByPatternResponse>(connectionResult.Error);
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

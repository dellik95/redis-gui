using RedisGUI.Domain.Audit;
using System;
using System.Collections.Generic;
using RedisGUI.Shared.Mediator;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;

/// <summary>
/// Command to delete multiple Redis keys by their exact names.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Keys">List of keys to delete.</param>
/// <param name="Database">Optional database index.</param>
[Auditable(AuditAction.Delete, "Redis")]
public sealed record DeleteMultipleKeysCommand(
	Guid ConnectionId,
	IReadOnlyList<string> Keys,
	int? Database = null) : ICommand<DeleteMultipleKeysResponse>;

/// <summary>
/// Handles the command to delete multiple Redis keys.
/// </summary>
internal sealed class DeleteMultipleKeysCommandHandler : ICommandHandler<DeleteMultipleKeysCommand, DeleteMultipleKeysResponse>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisKeyService keyService;

	public DeleteMultipleKeysCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisKeyService keyService)
	{
		this.connectionRepository = connectionRepository;
		this.keyService = keyService;
	}

	public async Task<Result<DeleteMultipleKeysResponse>> Handle(
		DeleteMultipleKeysCommand request,
		CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(
			request.ConnectionId,
			cancellationToken);

		if (connectionResult.IsFailure)
		{
			return Result.Failure<DeleteMultipleKeysResponse>(connectionResult.Error);
		}

		if (connectionResult.Value.IsReadOnly)
		{
			return Result.Failure<DeleteMultipleKeysResponse>(DomainErrors.Connection.ReadOnlyMode);
		}

		var deleteResult = await keyService.DeleteMultipleKeysAsync(
			connectionResult.Value,
			request.Keys,
			cancellationToken,
			request.Database);

		if (deleteResult.IsFailure)
		{
			return Result.Failure<DeleteMultipleKeysResponse>(deleteResult.Error);
		}

		var (deletedCount, failedKeys) = deleteResult.Value;

		var response = new DeleteMultipleKeysResponse(
			deletedCount,
			request.Keys.Count,
			failedKeys);

		return Result.Success(response);
	}
}

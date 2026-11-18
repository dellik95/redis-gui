using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
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
/// <param name="KeysBase64">Optional parallel list of Base64-encoded raw Redis keys.</param>
[Auditable(AuditAction.Delete, "Redis")]
public sealed record DeleteMultipleKeysCommand(
	Guid ConnectionId,
	IReadOnlyList<string> Keys,
	int? Database = null,
	IReadOnlyList<string?>? KeysBase64 = null) : ICommand<DeleteMultipleKeysResponse>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

/// <summary>
/// Handles the command to delete multiple Redis keys.
/// </summary>
internal sealed class DeleteMultipleKeysCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisKeyService keyService,
	IConnectionAccessService access) : ICommandHandler<DeleteMultipleKeysCommand, DeleteMultipleKeysResponse>
{
	public async Task<Result<DeleteMultipleKeysResponse>> Handle(
		DeleteMultipleKeysCommand request,
		CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true);

		if (connectionResult.IsFailure)
		{
			return Result.Failure<DeleteMultipleKeysResponse>(connectionResult.Error);
		}

		var deleteResult = request.KeysBase64 is null || request.KeysBase64.Count == 0
			? await keyService.DeleteMultipleKeysAsync(
				connectionResult.Value,
				request.Keys,
				cancellationToken,
				request.Database)
			: await keyService.DeleteMultipleKeysByIdentityAsync(
				connectionResult.Value,
				request.Keys,
				request.KeysBase64,
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

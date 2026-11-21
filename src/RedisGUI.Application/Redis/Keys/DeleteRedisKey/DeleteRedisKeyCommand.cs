using FluentResults;
using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using System;
using DispatchWeave;
using DispatchWeave.Results;
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
/// <param name="KeyBase64">Optional Base64-encoded raw Redis key bytes.</param>
[Auditable(AuditAction.Delete, "Redis")]
public sealed record DeleteRedisKeyCommand(Guid ConnectionId, string Key, int? Database = null, string? KeyBase64 = null) : ICommand<bool>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

/// <summary>
/// Handles the command to delete a Redis key.
/// </summary>
internal sealed class DeleteRedisKeyCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisKeyService keyService,
	IConnectionAccessService? access = null) : ICommandHandler<DeleteRedisKeyCommand, bool>
{
	public async Task<Result<bool>> Handle(DeleteRedisKeyCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true);

		if (connectionResult.IsFailed)
		{
			return Result.Fail<bool>(connectionResult.Errors);
		}

		return string.IsNullOrEmpty(request.KeyBase64)
			? await keyService.DeleteKeyAsync(
				connectionResult.Value,
				request.Key,
				cancellationToken,
				request.Database)
			: await keyService.DeleteKeyByIdentityAsync(
				connectionResult.Value,
				request.Key,
				request.KeyBase64,
				cancellationToken,
				request.Database);
	}
}

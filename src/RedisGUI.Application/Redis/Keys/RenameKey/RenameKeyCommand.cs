using FluentResults;
using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
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
	int? Database = null,
	string? OldKeyBase64 = null,
	string? NewKeyBase64 = null
) : ICommand, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

internal sealed class RenameKeyCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisValueService valueService,
	IConnectionAccessService? access = null) : ICommandHandler<RenameKeyCommand>
{
	public async Task<Result> Handle(RenameKeyCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true);
		if (connectionResult.IsFailed)
		{
			return Result.Fail(connectionResult.Errors);
		}

		return string.IsNullOrEmpty(request.OldKeyBase64) && string.IsNullOrEmpty(request.NewKeyBase64)
			? await valueService.RenameKeyAsync(
				connectionResult.Value,
				request.OldKey,
				request.NewKey,
				request.Overwrite,
				request.Database,
				cancellationToken)
			: await valueService.RenameKeyByIdentityAsync(
				connectionResult.Value,
				request.OldKey,
				request.OldKeyBase64,
				request.NewKey,
				request.NewKeyBase64,
				request.Overwrite,
				request.Database,
				cancellationToken);
	}
}

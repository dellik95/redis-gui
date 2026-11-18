using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using RedisGUI.Shared.Mediator;
using System;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.DuplicateKey;

/// <summary>
/// Command to duplicate a Redis key with all its internal data and TTL intact
/// </summary>
[Auditable(AuditAction.Create, "Redis")]
public sealed record DuplicateKeyCommand(
	Guid ConnectionId,
	string SourceKey,
	string TargetKey,
	int? Database = null,
	string? SourceKeyBase64 = null,
	string? TargetKeyBase64 = null
) : ICommand, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

internal sealed class DuplicateKeyCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisValueService valueService,
	IConnectionAccessService? access = null) : ICommandHandler<DuplicateKeyCommand>
{
	public async Task<Result> Handle(DuplicateKeyCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true);
		if (connectionResult.IsFailure)
		{
			return Result.Failure(connectionResult.Error);
		}

		return string.IsNullOrEmpty(request.SourceKeyBase64) && string.IsNullOrEmpty(request.TargetKeyBase64)
			? await valueService.DuplicateKeyAsync(
				connectionResult.Value,
				request.SourceKey,
				request.TargetKey,
				request.Database,
				cancellationToken)
			: await valueService.DuplicateKeyByIdentityAsync(
				connectionResult.Value,
				request.SourceKey,
				request.SourceKeyBase64,
				request.TargetKey,
				request.TargetKeyBase64,
				request.Database,
				cancellationToken);
	}
}

using FluentResults;
using System.Linq;
using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
using System;
using System.Collections.Generic;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.BulkUpdateTtl;

/// <summary>
/// Command to update TTL or persist multiple keys in bulk
/// </summary>
[Auditable(AuditAction.Update, "Redis")]
public sealed record BulkUpdateTtlCommand(
	Guid ConnectionId,
	IEnumerable<string> Keys,
	long? TtlSeconds,
	int? Database = null,
	IReadOnlyList<string?>? KeysBase64 = null
) : ICommand<RedisBulkTtlResult>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

internal sealed class BulkUpdateTtlCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisValueService valueService,
	IConnectionAccessService access) : ICommandHandler<BulkUpdateTtlCommand, RedisBulkTtlResult>
{
	public async Task<Result<RedisBulkTtlResult>> Handle(BulkUpdateTtlCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(
			request.ConnectionId,
			access,
			cancellationToken,
			requireWritable: true);
		if (connectionResult.IsFailed)
		{
			return Result.Fail<RedisBulkTtlResult>(connectionResult.Errors);
		}

		TimeSpan? ttl = request.TtlSeconds.HasValue && request.TtlSeconds.Value > 0
			? TimeSpan.FromSeconds(request.TtlSeconds.Value)
			: null;

		if (request.KeysBase64 is null || request.KeysBase64.Count == 0)
		{
			return await valueService.BulkUpdateTtlAsync(
				connectionResult.Value,
				request.Keys,
				ttl,
				request.Database,
				cancellationToken);
		}

		var keys = request.Keys as IReadOnlyList<string> ?? request.Keys.ToArray();
		return await valueService.BulkUpdateTtlByIdentityAsync(
			connectionResult.Value,
			keys,
			request.KeysBase64,
			ttl,
			request.Database,
			cancellationToken);
	}
}

using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using RedisGUI.Shared.Mediator;
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
	int? Database = null
) : ICommand<RedisBulkTtlResult>;

internal sealed class BulkUpdateTtlCommandHandler : ICommandHandler<BulkUpdateTtlCommand, RedisBulkTtlResult>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisValueService valueService;
	private readonly IConnectionAccessService access;

	public BulkUpdateTtlCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisValueService valueService, IConnectionAccessService access)
	{
		this.connectionRepository = connectionRepository;
		this.valueService = valueService;
		this.access = access;
	}

	public async Task<Result<RedisBulkTtlResult>> Handle(BulkUpdateTtlCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<RedisBulkTtlResult>(connectionResult.Error);
		}

		if (!await access.CanAccessAsync(connectionResult.Value))
			return Result.Failure<RedisBulkTtlResult>(new Error("Connection.Forbidden", "Access to the connection is denied."));

		if (connectionResult.Value.IsReadOnly)
		{
			return Result.Failure<RedisBulkTtlResult>(DomainErrors.Connection.ReadOnlyMode);
		}

		TimeSpan? ttl = request.TtlSeconds.HasValue && request.TtlSeconds.Value > 0
			? TimeSpan.FromSeconds(request.TtlSeconds.Value)
			: null;

		return await valueService.BulkUpdateTtlAsync(
			connectionResult.Value,
			request.Keys,
			ttl,
			request.Database,
			cancellationToken);
	}
}

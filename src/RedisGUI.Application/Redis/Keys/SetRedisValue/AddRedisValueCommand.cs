using FluentResults;
using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using System;
using DispatchWeave;
using DispatchWeave.Results;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.SetRedisValue;

/// <summary>
/// Command to set a Redis key-value pair with optional TTL.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Key">The key to set.</param>
/// <param name="Value">The value to set.</param>
/// <param name="Ttl">Optional time-to-live for the key.</param>
[Auditable(AuditAction.Create, "Redis")]
public sealed record AddRedisValueCommand(
	Guid ConnectionId,
	string Key,
	string Value,
	TimeSpan? Ttl = null) : ICommand, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

/// <summary>
/// Handles the command to set a Redis key-value pair.
/// </summary>
internal sealed class AddRedisValueCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisValueService valueService,
	IConnectionAccessService? access = null) : ICommandHandler<AddRedisValueCommand>
{
	public async Task<Result> Handle(AddRedisValueCommand request, CancellationToken cancellationToken)
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

		return await valueService.SetValueAsync(connectionResult.Value, request.Key, request.Value, request.Ttl, cancellationToken);
	}
}

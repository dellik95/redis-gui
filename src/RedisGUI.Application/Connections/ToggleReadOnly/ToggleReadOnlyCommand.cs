using FluentResults;
using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
using System;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Connections.ToggleReadOnly;

/// <summary>
/// Command to toggle safe read-only mode for a connection
/// </summary>
[Auditable(AuditAction.Update, "Connection")]
public sealed record ToggleReadOnlyCommand(Guid ConnectionId, bool IsReadOnly) : ICommand<bool>, IAuditEntityRequest
{
	public Guid? AuditEntityId => ConnectionId;
}

internal sealed class ToggleReadOnlyCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IUnitOfWork unitOfWork,
	IConnectionAccessService? access = null) : ICommandHandler<ToggleReadOnlyCommand, bool>
{
	public async Task<Result<bool>> Handle(ToggleReadOnlyCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
		if (connectionResult.IsFailed)
		{
			return Result.Fail<bool>(connectionResult.Errors);
		}

		var connection = connectionResult.Value;
		connection.SetReadOnly(request.IsReadOnly);

		connectionRepository.Update(connection);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok(connection.IsReadOnly);
	}
}

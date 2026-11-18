using RedisGUI.Application.Core.Services;
using System;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.DeleteConnection;

/// <summary>
/// Command to delete connection by specified Id
/// </summary>
/// <param name="Id">Connection identifier</param>
public sealed record DeleteConnectionCommand(Guid Id) : ICommand;

/// <summary>
/// Handles redis connection deletion
/// </summary>
internal sealed class DeleteConnectionCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IUnitOfWork unitOfWork,
	IConnectionAccessService? access = null) : ICommandHandler<DeleteConnectionCommand>
{
	public async Task<Result> Handle(DeleteConnectionCommand command, CancellationToken cancellationToken)
	{
		if (access is not null)
		{
			var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(command.Id, access, cancellationToken);
			if (connectionResult.IsFailure)
			{
				return Result.Failure(connectionResult.Error);
			}
		}

		var result = await connectionRepository.DeleteAsync(command.Id, cancellationToken);

		if (result.IsSuccess)
		{
			await unitOfWork.SaveChangesAsync(cancellationToken);
		}

		return result;
	}
}

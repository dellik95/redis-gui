using RedisGUI.Domain.Audit;
using RedisGUI.Application.Core.Services;
using System;
using RedisGUI.Shared.Mediator;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.UpdateConnection;

/// <summary>
/// Command to update an existing Redis connection with the specified parameters.
/// </summary>
[Auditable(AuditAction.Update, "Connection")]
public sealed record UpdateConnectionCommand(
	Guid Id,
	string Name,
	string Host,
	int Port,
	int Database,
	string? UserName = null,
	string? Password = null,
	bool IsReadOnly = false) : ICommand, IAuditEntityRequest
{
	public Guid? AuditEntityId => Id;
}

/// <summary>
/// Handles the updating of existing Redis connections, including validation and persistence.
/// </summary>
internal sealed class UpdateConnectionCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IPasswordEncryptor passwordEncryptor,
	IUnitOfWork unitOfWork,
	IRedisConnectionService connectionService,
	IConnectionAccessService? access = null) : ICommandHandler<UpdateConnectionCommand>
{
	public async Task<Result> Handle(UpdateConnectionCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.Id, access, cancellationToken);

		if (connectionResult.IsFailure)
		{
			return connectionResult;
		}

		var existingConnection = connectionResult.Value;

		var updateResult = existingConnection.UpdateConnectionDetails(
			new ConnectionName(request.Name),
			new ConnectionHost(request.Host),
			new ConnectionPort(request.Port),
			new DatabaseNumber(request.Database));

		if (updateResult.IsFailure)
		{
			return updateResult;
		}

		existingConnection.SetReadOnly(request.IsReadOnly);

		if (existingConnection is RedisConnectionWithCredentials connectionWithCredentials)
		{
			var credentialsResult = ConnectionCredentials.Create(
				request.UserName,
				request.Password,
				passwordEncryptor);

			if (credentialsResult.IsSuccess && credentialsResult.Value is not null)
			{
				connectionWithCredentials.UpdateCredentials(credentialsResult.Value);
			}
		}

		var testResult = await connectionService.CheckConnectionAsync(existingConnection, cancellationToken);
		existingConnection.UpdateAvailability(testResult.IsSuccess);

		connectionRepository.Update(existingConnection);
		try
		{
			await unitOfWork.SaveChangesAsync(cancellationToken);
			return Result.Success();
		}
		catch (RedisGUI.Domain.Exceptions.ErrorException ex)
		{
			return Result.Failure(ex.Error);
		}
	}
}

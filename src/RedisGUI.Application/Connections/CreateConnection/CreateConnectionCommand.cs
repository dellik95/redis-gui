using RedisGUI.Domain.Audit;
using System;
using RedisGUI.Shared.Mediator;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Connections.CreateConnection;

/// <summary>
/// Command to create a new Redis connection with the specified parameters.
/// </summary>
/// <param name="Name">The name of the connection</param>
/// <param name="Host">The Redis server host address</param>
/// <param name="Port">The Redis server port number</param>
/// <param name="Database">The Redis database number to connect to</param>
/// <param name="UserName">The username for authentication (optional)</param>
/// <param name="Password">The password for authentication (optional)</param>
/// <param name="IsReadOnly">Whether the connection is in Safe Read-Only mode</param>
[Auditable(AuditAction.Create, "Connection")]
public sealed record CreateConnectionCommand(
	string Name,
	string Host,
	int Port,
	int Database,
	string? UserName = null,
	string? Password = null,
	bool IsReadOnly = false) : ICommand<Guid>;

/// <summary>
/// Handles the creation of new Redis connections, including validation and persistence.
/// </summary>
internal sealed class CreateConnectionCommandHandler(
	IRedisConnectionRepository connectionRepository,
	IPasswordEncryptor passwordEncryptor,
	IUnitOfWork unitOfWork,
	IRedisConnectionService connectionService,
	ICurrentUserService currentUserService)
	: ICommandHandler<CreateConnectionCommand, Guid>
{
	/// <summary>
	/// Handles the creation of a new Redis connection, validates it, and persists it to the repository.
	/// </summary>
	public async Task<Result<Guid>> Handle(CreateConnectionCommand request, CancellationToken cancellationToken)
	{
		Result<ConnectionCredentials?> credentialsResult = ConnectionCredentials.Create(
			request.UserName,
			request.Password,
			passwordEncryptor);

		if (credentialsResult.IsFailure)
			return Result.Failure<Guid>(credentialsResult.Error);

		Guid? userId = currentUserService.UserId;
		if (!userId.HasValue)
		{
			return Result.Failure<Guid>(Domain.Errors.DomainErrors.User.Unauthorized);
		}

		DatabaseNumber databaseNumber = new(request.Database);
		ConnectionOwnerId ownerId = ConnectionOwnerId.Some(userId.Value);

		Result<RedisConnection> createResult = RedisConnection.Create(
			new ConnectionName(request.Name),
			new ConnectionHost(request.Host),
			new ConnectionPort(request.Port),
			databaseNumber,
			ownerId,
			credentialsResult.Value);

		if (createResult.IsFailure)
			return Result.Failure<Guid>(createResult.Error);

		createResult.Value.SetReadOnly(request.IsReadOnly);

		Result testResult = await connectionService.CheckConnectionAsync(createResult.Value, cancellationToken);
		if (testResult.IsFailure)
			return Result.Failure<Guid>(testResult.Error);
		createResult.Value.UpdateAvailability(testResult.IsSuccess);

		connectionRepository.Add(createResult.Value);
		Result<int> saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

		if (saveResult.IsFailure)
		{
			return Result.Failure<Guid>(saveResult.Error);
		}

		return Result.Success(createResult.Value.Id);
	}
}

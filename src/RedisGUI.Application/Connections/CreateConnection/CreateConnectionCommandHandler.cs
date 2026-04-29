using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Connections.CreateConnection;

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
	/// <param name="request">The connection creation command</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the new connection's ID if successful</returns>
	public async Task<Result<Guid>> Handle(CreateConnectionCommand request, CancellationToken cancellationToken)
	{
		// Create and validate credentials
		Result<ConnectionCredentials?> credentialsResult = ConnectionCredentials.Create(
			request.UserName,
			request.Password,
			passwordEncryptor);

		if (credentialsResult.IsFailure)
			return Result.Failure<Guid>(credentialsResult.Error);

		// Get current user ID for connection ownership
		Guid? userId = currentUserService.UserId;
		if (!userId.HasValue)
		{
			return Result.Failure<Guid>(Domain.Errors.DomainErrors.User.Unauthorized);
		}

		// Create value objects
		DatabaseNumber databaseNumber = new(request.Database);
		ConnectionOwnerId ownerId = ConnectionOwnerId.Some(userId.Value);

		// Create connection with value objects
		Result<RedisConnection> createResult = RedisConnection.Create(
			new ConnectionName(request.Name),
			new ConnectionHost(request.Host),
			new ConnectionPort(request.Port),
			databaseNumber,
			ownerId,
			credentialsResult.Value);

		if (createResult.IsFailure)
			return Result.Failure<Guid>(createResult.Error);

		// Test the connection
		Result testResult = await connectionService.CheckConnectionAsync(createResult.Value, cancellationToken);
		createResult.Value.UpdateAvailability(testResult.IsSuccess);

		// Save to repository
		connectionRepository.Add(createResult.Value);
		Result<int> saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

		if (saveResult.IsFailure)
		{
			return Result.Failure<Guid>(saveResult.Error);
		}

		return Result.Success(createResult.Value.Id);
	}
}

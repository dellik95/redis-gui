using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Connections.CreateConnection;

/// <summary>
/// Handles the creation of new Redis connections, including validation and persistence.
/// </summary>
internal sealed class CreateConnectionCommandHandler : ICommandHandler<CreateConnectionCommand, Guid>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IPasswordEncryptor passwordEncryptor;
	private readonly IUnitOfWork unitOfWork;
	private readonly IConnectionService connectionService;

	/// <summary>
	/// Initializes a new instance of the CreateConnectionCommandHandler.
	/// </summary>
	/// <param name="connectionRepository">Repository for Redis connections</param>
	/// <param name="passwordEncryptor">Service for password encryption</param>
	/// <param name="unitOfWork">Unit of work for transaction management</param>
	/// <param name="connectionService">Service for testing Redis connections</param>
	public CreateConnectionCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IPasswordEncryptor passwordEncryptor,
		IUnitOfWork unitOfWork,
		IConnectionService connectionService)
	{
		this.connectionRepository = connectionRepository;
		this.passwordEncryptor = passwordEncryptor;
		this.unitOfWork = unitOfWork;
		this.connectionService = connectionService;
	}

	/// <summary>
	/// Handles the creation of a new Redis connection, validates it, and persists it to the repository.
	/// </summary>
	/// <param name="request">The connection creation command</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the new connection's ID if successful</returns>
	public async Task<Result<Guid>> Handle(CreateConnectionCommand request, CancellationToken cancellationToken)
	{
		// Create and validate credentials
		var credentialsResult = ConnectionCredentials.Create(
			request.UserName,
			request.Password,
			passwordEncryptor);

		if (credentialsResult.IsFailure)
		{
			return Result.Failure<Guid>(credentialsResult.Error);
		}

		// Create connection with value objects
		var connectionResult = RedisConnection.Create(
			new ConnectionName(request.Name),
			new ConnectionHost(request.Host),
			new ConnectionPort(request.Port),
			request.Database,
			credentialsResult.Value);

		// Test the connection before saving
		var connectionTest = await connectionService.CheckConnection(connectionResult);
		if (connectionTest.IsFailure)
		{
			return Result.Failure<Guid>(connectionTest.Error);
		}

		// Save to repository
		connectionRepository.Add(connectionResult);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return connectionResult.Value.Id;
	}
}

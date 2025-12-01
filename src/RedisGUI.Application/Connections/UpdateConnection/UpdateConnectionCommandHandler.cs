using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Extensions;
using RedisGUI.Shared.Primitives;
using RedisGUI.Shared.Mediator;
using RedisGUI.Domain.Errors;

namespace RedisGUI.Application.Connections.UpdateConnection;

/// <summary>
/// Handles the updating of existing Redis connections, including validation and persistence.
/// </summary>
internal sealed class UpdateConnectionCommandHandler : ICommandHandler<UpdateConnectionCommand>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IPasswordEncryptor passwordEncryptor;
	private readonly IUnitOfWork unitOfWork;
	private readonly IConnectionService connectionService;

	/// <summary>
	/// Initializes a new instance of the UpdateConnectionCommandHandler.
	/// </summary>
	/// <param name="connectionRepository">Repository for Redis connections</param>
	/// <param name="passwordEncryptor">Service for password encryption</param>
	/// <param name="unitOfWork">Unit of work for transaction management</param>
	/// <param name="connectionService">Service for testing Redis connections</param>
	public UpdateConnectionCommandHandler(
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
	/// Handles the update of an existing Redis connection, validates it, and persists changes.
	/// </summary>
	/// <param name="request">The connection update command</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure of the update operation</returns>
	public async Task<Result> Handle(UpdateConnectionCommand request, CancellationToken cancellationToken)
	{
		// Retrieve existing connection
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.Id, cancellationToken);
		
		if (connectionResult.IsFailure)
		{
			return connectionResult;
		}

		var existingConnection = connectionResult.Value;

		// Update connection details
		var updateResult = existingConnection.UpdateConnectionDetails(
			new ConnectionName(request.Name),
			new ConnectionHost(request.Host),
			new ConnectionPort(request.Port),
			request.Database);

		if (updateResult.IsFailure)
		{
			return updateResult;
		}

		// Update credentials if provided
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

		// Test connection and update availability
		var testResult = await connectionService.CheckConnection(existingConnection);
		existingConnection.UpdateAvailability(testResult.IsSuccess);

		// Save changes
		connectionRepository.Update(existingConnection);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success();
	}
}

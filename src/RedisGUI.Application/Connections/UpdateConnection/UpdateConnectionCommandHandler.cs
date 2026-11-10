using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Shared.Primitives;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Connections.UpdateConnection;

/// <summary>
/// Handles the updating of existing Redis connections, including validation and persistence.
/// </summary>
internal sealed class UpdateConnectionCommandHandler : ICommandHandler<UpdateConnectionCommand>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IPasswordEncryptor passwordEncryptor;
	private readonly IUnitOfWork unitOfWork;
	private readonly IRedisConnectionService connectionService;

	public UpdateConnectionCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IPasswordEncryptor passwordEncryptor,
		IUnitOfWork unitOfWork,
		IRedisConnectionService connectionService)
	{
		this.connectionRepository = connectionRepository;
		this.passwordEncryptor = passwordEncryptor;
		this.unitOfWork = unitOfWork;
		this.connectionService = connectionService;
	}

	public async Task<Result> Handle(UpdateConnectionCommand request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.Id, cancellationToken);
		
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

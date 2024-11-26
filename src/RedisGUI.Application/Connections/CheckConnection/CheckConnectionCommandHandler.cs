using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Connections.CheckConnection;

/// <summary>
/// Handles the validation and testing of Redis connection parameters.
/// </summary>
internal sealed class CheckConnectionCommandHandler : ICommandHandler<CheckConnectionCommand>
{
	private readonly IConnectionService connectionService;
	private readonly IPasswordEncryptor passwordEncryptor;

	/// <summary>
	/// Initializes a new instance of the CheckConnectionCommandHandler.
	/// </summary>
	/// <param name="connectionService">Service for testing Redis connections</param>
	/// <param name="passwordEncryptor">Service for password encryption</param>
	public CheckConnectionCommandHandler(
		IConnectionService connectionService,
		IPasswordEncryptor passwordEncryptor)
	{
		this.connectionService = connectionService;
		this.passwordEncryptor = passwordEncryptor;
	}

	/// <summary>
	/// Tests the Redis connection with the provided parameters.
	/// </summary>
	/// <param name="request">The connection test command</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure of the connection test</returns>
	public async Task<Result> Handle(CheckConnectionCommand request, CancellationToken cancellationToken)
	{
		var credentialsResult =  ConnectionCredentials.Create(
			request.UserName, 
			request.Password, 
			passwordEncryptor);


		var connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost(request.Host),
			new ConnectionPort(request.Port),
			request.Database,
			credentialsResult.Value);

		var result = await connectionService.CheckConnection(connection);

		return result;
	}
}

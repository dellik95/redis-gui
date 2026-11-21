using FluentResults;
using DispatchWeave;
using DispatchWeave.Results;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Connections.CheckConnection;

/// <summary>
/// Command to test a Redis connection with the specified parameters.
/// </summary>
/// <param name="Host">The Redis server host address</param>
/// <param name="Port">The Redis server port number</param>
/// <param name="Database">The Redis database number to connect to (optional)</param>
/// <param name="UserName">The username for authentication (optional)</param>
/// <param name="Password">The password for authentication (optional)</param>
public sealed record CheckConnectionCommand(
	string Host,
	int Port,
	int Database = 0,
	string? UserName = null,
	string? Password = null) : ICommand;

/// <summary>
/// Handles the validation and testing of Redis connection parameters.
/// </summary>
internal sealed class CheckConnectionCommandHandler(
	IRedisConnectionService connectionService,
	IPasswordEncryptor passwordEncryptor)
	: ICommandHandler<CheckConnectionCommand>
{

	/// <summary>
	/// Tests the Redis connection with the provided parameters.
	/// </summary>
	/// <param name="request">The connection test command</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result indicating success or failure of the connection test</returns>
	public async Task<Result> Handle(CheckConnectionCommand request, CancellationToken cancellationToken)
	{
		Result<ConnectionCredentials?> credentialsResult = ConnectionCredentials.Create(
			request.UserName,
			request.Password,
			passwordEncryptor);


		Result<RedisConnection> connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost(request.Host),
			new ConnectionPort(request.Port),
			new DatabaseNumber(request.Database),
			ConnectionOwnerId.None(), // ownerId - temporary test connection
			credentialsResult.Value);

		if (connection.IsFailed)
            return connection.ToResult();

        Result result = await connectionService.CheckConnectionAsync(connection.Value, cancellationToken);

		return result;
	}
}

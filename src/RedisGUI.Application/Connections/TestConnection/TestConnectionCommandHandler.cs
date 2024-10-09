using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Connections.TestConnection;

internal sealed class TestConnectionCommandHandler : ICommandHandler<TestConnectionCommand>
{
	private readonly IConnectionService _connectionService;
	private readonly IPasswordEncryptor _passwordEncryptor;

	public TestConnectionCommandHandler(IConnectionService connectionService, IPasswordEncryptor passwordEncryptor)
	{
		_connectionService = connectionService;
		_passwordEncryptor = passwordEncryptor;
	}
	public async Task<Result> Handle(TestConnectionCommand request, CancellationToken cancellationToken)
	{
		var credentials = ConnectionCredentials.Create(request.UserName, request.Password, this._passwordEncryptor);

		if (credentials.IsFailure)
		{
			return Result.Failure<Guid>(credentials.Error);
		}

		var connection = RedisConnection.Create(
			new ConnectionName(request.Name),
			new ConnectionHost(request.Host),
			new ConnectionPort(request.Port),
			request.Database,
			credentials);

		return await this._connectionService.TestConnection(connection);
	}
}
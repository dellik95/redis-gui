using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Connections.CreateConnection;

internal sealed class CreateConnectionCommandHandler : ICommandHandler<CreateConnectionCommand, Guid>
{
	private readonly IRedisConnectionRepository _connectionRepository;
	private readonly IUnitOfWork _unitOfWork;
	private readonly IPasswordEncryptor _passwordEncryptor;

	public CreateConnectionCommandHandler(IRedisConnectionRepository connectionRepository, IUnitOfWork unitOfWork, IPasswordEncryptor passwordEncryptor)
	{
		this._connectionRepository = connectionRepository;
		_unitOfWork = unitOfWork;
		_passwordEncryptor = passwordEncryptor;
	}

	public async Task<Result<Guid>> Handle(CreateConnectionCommand request, CancellationToken cancellationToken)
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

		this._connectionRepository.Add(connection);

		await this._unitOfWork.SaveChangesAsync(cancellationToken);

		return connection.Id;
	}
}
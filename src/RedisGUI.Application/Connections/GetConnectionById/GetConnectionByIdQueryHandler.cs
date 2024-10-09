using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Connections.GetConnectionById;

internal sealed class GetConnectionByIdQueryHandler : IQueryHandler<GetConnectionByIdQuery, GetConnectionByIdResponse>
{
	private readonly IRedisConnectionRepository _connectionRepository;
	private readonly IPasswordDecryptor _passwordDecryptor;

	public GetConnectionByIdQueryHandler(IRedisConnectionRepository connectionRepository, IPasswordDecryptor passwordDecryptor)
	{
		_connectionRepository = connectionRepository;
		_passwordDecryptor = passwordDecryptor;
	}

	public async Task<Result<GetConnectionByIdResponse>> Handle(GetConnectionByIdQuery request, CancellationToken cancellationToken)
	{
		var connection = await this._connectionRepository.GetConnectionByIdAsync(request.id, cancellationToken);
		return connection.Map(x => GetConnectionByIdResponse.FromRedisConnection(x, _passwordDecryptor));
	}
}
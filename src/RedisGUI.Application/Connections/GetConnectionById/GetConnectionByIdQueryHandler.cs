using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Application.Connections.Shared;

namespace RedisGUI.Application.Connections.GetConnectionById;

/// <summary>
/// Handles the retrieval of Redis connections by their unique identifier.
/// </summary>
internal sealed class GetConnectionByIdQueryHandler : IQueryHandler<GetConnectionByIdQuery, GetConnectionResponse>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IPasswordDecrypt passwordDecrypt;

	/// <summary>
	/// Initializes a new instance of the GetConnectionByIdQueryHandler.
	/// </summary>
	/// <param name="connectionRepository">Repository for Redis connections</param>
	/// <param name="passwordDecrypt">Service for password decryption</param>
	public GetConnectionByIdQueryHandler(IRedisConnectionRepository connectionRepository, IPasswordDecrypt passwordDecrypt)
	{
		this.connectionRepository = connectionRepository;
		this.passwordDecrypt = passwordDecrypt;
	}

	/// <summary>
	/// Retrieves a Redis connection by its ID and returns it as a response object.
	/// </summary>
	/// <param name="request">The connection retrieval query</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the connection details if found</returns>
	public async Task<Result<GetConnectionResponse>> Handle(GetConnectionByIdQuery request, CancellationToken cancellationToken)
	{
		var connection = await connectionRepository.GetConnectionByIdAsync(request.Id, cancellationToken);
		return connection.Map(x => GetConnectionResponse.FromRedisConnection(x, passwordDecrypt));
	}
}

using System;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Shared.Mediator;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetConnectionById;

/// <summary>
/// Query to retrieve a Redis connection by its unique identifier.
/// </summary>
/// <param name="Id">The unique identifier of the connection to retrieve</param>
public sealed record GetConnectionByIdQuery(Guid Id) : IQuery<GetConnectionResponse>;

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

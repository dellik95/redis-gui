﻿using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Connections.GetConnectionById;

/// <summary>
/// Handles the retrieval of Redis connections by their unique identifier.
/// </summary>
internal sealed class GetConnectionByIdQueryHandler : IQueryHandler<GetConnectionByIdQuery, GetConnectionByIdResponse>
{
	private readonly IRedisConnectionRepository _connectionRepository;
	private readonly IPasswordDecryptor _passwordDecryptor;

	/// <summary>
	/// Initializes a new instance of the GetConnectionByIdQueryHandler.
	/// </summary>
	/// <param name="connectionRepository">Repository for Redis connections</param>
	/// <param name="passwordDecryptor">Service for password decryption</param>
	public GetConnectionByIdQueryHandler(IRedisConnectionRepository connectionRepository, IPasswordDecryptor passwordDecryptor)
	{
		_connectionRepository = connectionRepository;
		_passwordDecryptor = passwordDecryptor;
	}

	/// <summary>
	/// Retrieves a Redis connection by its ID and returns it as a response object.
	/// </summary>
	/// <param name="request">The connection retrieval query</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Result containing the connection details if found</returns>
	public async Task<Result<GetConnectionByIdResponse>> Handle(GetConnectionByIdQuery request, CancellationToken cancellationToken)
	{
		var connection = await _connectionRepository.GetConnectionByIdAsync(request.id, cancellationToken);
		return connection.Map(x => GetConnectionByIdResponse.FromRedisConnection(x, _passwordDecryptor));
	}
}

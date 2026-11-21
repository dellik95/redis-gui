using FluentResults;
using System;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
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
internal sealed class GetConnectionByIdQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IPasswordDecrypt passwordDecrypt,
	IRedisTopologyRegistry? topology = null,
	IConnectionAccessService? access = null) : IQueryHandler<GetConnectionByIdQuery, GetConnectionResponse>
{
	public async Task<Result<GetConnectionResponse>> Handle(GetConnectionByIdQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.Id, access, cancellationToken);
		if (connectionResult.IsFailed)
			return Result.Fail<GetConnectionResponse>(connectionResult.Errors);

		var serverMode = topology?.GetMode(request.Id) ?? RedisServerMode.Unknown;
		return Result.Ok(GetConnectionResponse.FromRedisConnection(connectionResult.Value, passwordDecrypt, serverMode));
	}
}

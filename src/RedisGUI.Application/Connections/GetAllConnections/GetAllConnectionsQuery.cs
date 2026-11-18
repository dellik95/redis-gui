using System.Collections.Generic;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Application.Core.Services;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetAllConnections;

/// <summary>
///    Query to retrieve a Redis connections.
/// </summary>
/// <param name="PageNumber">Page number</param>
/// <param name="PageSize">Items count per page</param>
public record GetAllConnectionsQuery(int PageNumber, int PageSize) : IQuery<IEnumerable<GetConnectionResponse>>;

internal sealed class GetAllConnectionsQueryHandler : IQueryHandler<GetAllConnectionsQuery, IEnumerable<GetConnectionResponse>>
{
	private readonly IRedisConnectionRepository redisConnectionRepository;
	private readonly IPasswordDecrypt passwordDecrypt;
	private readonly IRedisTopologyRegistry? topology;
	private readonly IConnectionAccessService? access;

	public GetAllConnectionsQueryHandler(
		IRedisConnectionRepository redisConnectionRepository,
		IPasswordDecrypt passwordDecrypt,
		IRedisTopologyRegistry? topology = null,
		IConnectionAccessService? access = null)
	{
		this.redisConnectionRepository = redisConnectionRepository;
		this.passwordDecrypt = passwordDecrypt;
		this.topology = topology;
		this.access = access;
	}


	public async Task<Result<IEnumerable<GetConnectionResponse>>> Handle(GetAllConnectionsQuery request, CancellationToken cancellationToken)
	{
		var connectionsResult = await this.redisConnectionRepository.GetAsync(null, false, request.PageNumber, request.PageSize);
		if (connectionsResult.IsFailure)
			return Result.Failure<IEnumerable<GetConnectionResponse>>(connectionsResult.Error);

		var responses = new List<GetConnectionResponse>();
		foreach (var connection in connectionsResult.Value.Items)
		{
			if (access is not null && !await access.CanAccessAsync(connection))
				continue;

			var serverMode = topology?.GetMode(connection.Id) ?? RedisServerMode.Unknown;
			responses.Add(GetConnectionResponse.FromRedisConnection(connection, passwordDecrypt, serverMode));
		}

		return Result.Success<IEnumerable<GetConnectionResponse>>(responses);
	}
}

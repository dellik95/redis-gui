using System;
using RedisGUI.Application.Core.Services;
using RedisGUI.Shared.Mediator;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Queries.GetKeyspaceInfo;

/// <summary>
/// Query to retrieve Redis keyspace information (key counts across databases DB 0-15)
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier</param>
public sealed record GetKeyspaceInfoQuery(Guid ConnectionId) : IQuery<GetKeyspaceInfoResponse>;

/// <summary>
/// Query handler for retrieving keyspace statistics across databases
/// </summary>
internal sealed class GetKeyspaceInfoQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisDiagnosticsService diagnosticsService,
	IConnectionAccessService? access = null) : IQueryHandler<GetKeyspaceInfoQuery, GetKeyspaceInfoResponse>
{
	public async Task<Result<GetKeyspaceInfoResponse>> Handle(GetKeyspaceInfoQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<GetKeyspaceInfoResponse>(connectionResult.Error);
		}

		var keyspaceResult = await diagnosticsService.GetKeyspaceInfoAsync(connectionResult.Value, cancellationToken);
		if (keyspaceResult.IsFailure)
		{
			return Result.Failure<GetKeyspaceInfoResponse>(keyspaceResult.Error);
		}

		return Result.Success(new GetKeyspaceInfoResponse(keyspaceResult.Value));
	}
}

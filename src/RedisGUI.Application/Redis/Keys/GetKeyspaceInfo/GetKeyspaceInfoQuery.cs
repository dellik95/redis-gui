using FluentResults;
using System;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
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
		if (connectionResult.IsFailed)
		{
			return Result.Fail<GetKeyspaceInfoResponse>(connectionResult.Errors);
		}

		var keyspaceResult = await diagnosticsService.GetKeyspaceInfoAsync(connectionResult.Value, cancellationToken);
		if (keyspaceResult.IsFailed)
		{
			return Result.Fail<GetKeyspaceInfoResponse>(keyspaceResult.Errors);
		}

		return Result.Ok(new GetKeyspaceInfoResponse(keyspaceResult.Value));
	}
}

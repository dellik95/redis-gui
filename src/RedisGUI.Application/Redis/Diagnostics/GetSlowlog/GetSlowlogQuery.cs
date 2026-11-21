using FluentResults;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Redis;
using DispatchWeave;
using DispatchWeave.Results;
using System;
using System.Collections.Generic;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.GetSlowlog;

/// <summary>
/// Query to retrieve Redis slowlog entries
/// </summary>
public sealed record GetSlowlogQuery(Guid ConnectionId, int Count = 25) : IQuery<List<SlowlogEntryInfo>>;

internal sealed class GetSlowlogQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisDiagnosticsService diagnosticsService,
	IConnectionAccessService? access = null) : IQueryHandler<GetSlowlogQuery, List<SlowlogEntryInfo>>
{
	public async Task<Result<List<SlowlogEntryInfo>>> Handle(GetSlowlogQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
		if (connectionResult.IsFailed)
		{
			return Result.Fail<List<SlowlogEntryInfo>>(connectionResult.Errors);
		}

		return await diagnosticsService.GetSlowlogAsync(connectionResult.Value, request.Count, cancellationToken);
	}
}

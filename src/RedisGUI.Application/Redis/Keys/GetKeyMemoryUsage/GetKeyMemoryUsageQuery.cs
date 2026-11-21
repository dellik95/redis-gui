using FluentResults;
using System;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Queries.GetKeyMemoryUsage;

/// <summary>
/// Query to retrieve memory usage in bytes for a specific Redis key
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier</param>
/// <param name="Key">The Redis key name</param>
/// <param name="Database">Optional logical database index</param>
/// <param name="KeyBase64">Optional Base64-encoded key bytes</param>
public sealed record GetKeyMemoryUsageQuery(
	Guid ConnectionId,
	string Key,
	int? Database = null,
	string? KeyBase64 = null) : IQuery<GetKeyMemoryUsageResponse>;

/// <summary>
/// Handler for retrieving memory usage of a specific key
/// </summary>
internal sealed class GetKeyMemoryUsageQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisDiagnosticsService diagnosticsService,
	IConnectionAccessService? access = null) : IQueryHandler<GetKeyMemoryUsageQuery, GetKeyMemoryUsageResponse>
{
	public async Task<Result<GetKeyMemoryUsageResponse>> Handle(GetKeyMemoryUsageQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
		if (connectionResult.IsFailed)
		{
			return Result.Fail<GetKeyMemoryUsageResponse>(connectionResult.Errors);
		}

		var memoryResult = await diagnosticsService.GetKeyMemoryUsageAsync(
			connectionResult.Value,
			request.Key,
			cancellationToken,
			request.Database,
			request.KeyBase64);

		if (memoryResult.IsFailed)
		{
			return Result.Fail<GetKeyMemoryUsageResponse>(memoryResult.Errors);
		}

		return Result.Ok(new GetKeyMemoryUsageResponse(request.Key, memoryResult.Value));
	}
}

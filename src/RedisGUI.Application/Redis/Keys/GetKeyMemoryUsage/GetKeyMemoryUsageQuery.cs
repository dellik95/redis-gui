using System;
using RedisGUI.Shared.Mediator;
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
public sealed record GetKeyMemoryUsageQuery(
	Guid ConnectionId,
	string Key) : IQuery<GetKeyMemoryUsageResponse>;

/// <summary>
/// Handler for retrieving memory usage of a specific key
/// </summary>
internal sealed class GetKeyMemoryUsageQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisDiagnosticsService diagnosticsService) : IQueryHandler<GetKeyMemoryUsageQuery, GetKeyMemoryUsageResponse>
{
	public async Task<Result<GetKeyMemoryUsageResponse>> Handle(GetKeyMemoryUsageQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken);
		if (connectionResult.IsFailure)
		{
			return Result.Failure<GetKeyMemoryUsageResponse>(connectionResult.Error);
		}

		var memoryResult = await diagnosticsService.GetKeyMemoryUsageAsync(
			connectionResult.Value,
			request.Key,
			cancellationToken);

		if (memoryResult.IsFailure)
		{
			return Result.Failure<GetKeyMemoryUsageResponse>(memoryResult.Error);
		}

		return Result.Success(new GetKeyMemoryUsageResponse(request.Key, memoryResult.Value));
	}
}

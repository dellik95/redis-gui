using System;
using RedisGUI.Application.Core.Services;
using RedisGUI.Shared.Mediator;
using RedisGUI.Domain.Errors;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetConnectionHealth;

/// <summary>
/// Query to get health status of a connection
/// </summary>
/// <param name="ConnectionId">The connection identifier</param>
public sealed record GetConnectionHealthQuery(Guid ConnectionId) : IQuery<GetConnectionHealthResponse>;

/// <summary>
/// Handler for getting connection health status
/// </summary>
public sealed class GetConnectionHealthQueryHandler(
	IConnectionHealthMonitor healthMonitor,
	IRedisConnectionRepository? connectionRepository = null,
	IConnectionAccessService? access = null,
	IRedisTopologyRegistry? topology = null) : IQueryHandler<GetConnectionHealthQuery, GetConnectionHealthResponse>
{
	public async Task<Result<GetConnectionHealthResponse>> Handle(GetConnectionHealthQuery request, CancellationToken cancellationToken)
	{
		if (connectionRepository is not null && access is not null)
		{
			var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);
			if (connectionResult.IsFailure)
				return Result.Failure<GetConnectionHealthResponse>(connectionResult.Error);
		}

		var health = healthMonitor.GetHealth(request.ConnectionId);

		if (health == null)
		{
			return Result.Failure<GetConnectionHealthResponse>(
				DomainErrors.Health.NotFound);
		}

		var mode = topology?.GetMode(request.ConnectionId) ?? RedisServerMode.Unknown;
		string serverMode = mode != RedisServerMode.Unknown
			? mode.ToString()
			: health.ServerMode;

		var response = new GetConnectionHealthResponse(
			health.ConnectionId,
			health.Status.ToString(),
			health.LastChecked,
			health.ResponseTimeMs,
			health.ErrorMessage,
			health.ConsecutiveFailures,
			serverMode);

		return Result.Success(response);
	}
}

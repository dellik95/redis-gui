using System;
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
public sealed class GetConnectionHealthQueryHandler : IQueryHandler<GetConnectionHealthQuery, GetConnectionHealthResponse>
{
	private readonly IConnectionHealthMonitor healthMonitor;

	public GetConnectionHealthQueryHandler(IConnectionHealthMonitor healthMonitor)
	{
		this.healthMonitor = healthMonitor;
	}

	public Task<Result<GetConnectionHealthResponse>> Handle(GetConnectionHealthQuery request, CancellationToken cancellationToken)
	{
		var health = healthMonitor.GetHealth(request.ConnectionId);

		if (health == null)
		{
			return Task.FromResult(Result.Failure<GetConnectionHealthResponse>(
				DomainErrors.Health.NotFound));
		}

		var response = new GetConnectionHealthResponse(
			health.ConnectionId,
			health.Status.ToString(),
			health.LastChecked,
			health.ResponseTimeMs,
			health.ErrorMessage,
			health.ConsecutiveFailures);

		return Task.FromResult(Result.Success(response));
	}
}

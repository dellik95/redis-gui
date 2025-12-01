using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Connections.GetConnectionHealth;

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
				new Error("Health.NotFound", "No health information available for this connection")));
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

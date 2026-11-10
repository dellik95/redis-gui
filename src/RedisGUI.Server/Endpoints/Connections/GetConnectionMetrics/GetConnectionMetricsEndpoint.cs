using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.RedisMetrics.Queries;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.Connections.GetConnectionMetrics;

/// <summary>
/// Endpoint for retrieving instantaneous Redis system metrics for a connection.
/// </summary>
public class GetConnectionMetricsEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for getting instantaneous metrics.
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("connections/{connectionId:guid}/metrics", async (
			IMediator sender,
			Guid connectionId) =>
		{
			var query = new GetRedisMetricsQuery(connectionId);

			return await sender.Send<RedisGUI.Domain.RedisMetrics.RedisMetrics>(query)
				.Match(Results.Ok, Results.BadRequest);
		})
		.WithName("GetConnectionMetrics")
		.WithSummary("Get connection metrics")
		.WithDescription("Returns current Redis system metrics for a connection");
	}
}

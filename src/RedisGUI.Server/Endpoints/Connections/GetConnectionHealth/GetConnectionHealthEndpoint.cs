using DispatchWeave;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.GetConnectionHealth;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.Connections.GetConnectionHealth;

/// <summary>
/// Endpoint for getting connection health status
/// </summary>
public class GetConnectionHealthEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for getting connection health
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("connections/{connectionId:guid}/health", async (
			IDispatcher sender,
			Guid connectionId) =>
		{
			var query = new GetConnectionHealthQuery(connectionId);

			return await sender.Send(query)
				.Match(Results.Ok, error => error.ToProblemDetails());
		})
		.RequireAuthorization(Policies.Authenticated)
		.WithName("GetConnectionHealth")
		.WithSummary("Get connection health status")
		.WithDescription("Returns the current health status of a Redis connection");
	}
}

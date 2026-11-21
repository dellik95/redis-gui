using DispatchWeave;
using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.GetKeyspaceInfo;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetKeyspaceInfo;

/// <summary>
/// Endpoint for retrieving Redis keyspace information (key counts across databases)
/// </summary>
public sealed class GetKeyspaceInfoEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("storage/{connectionId:guid}/keyspace", async (
			IDispatcher sender,
			Guid connectionId,
			CancellationToken cancellationToken) =>
		{
			GetKeyspaceInfoQuery query = new(connectionId);
			var result = await sender.Send(query, cancellationToken);
			return result.ToHttpResult();
		})
		.WithName("GetKeyspaceInfo")
		.WithSummary("Get Redis keyspace statistics")
		.WithDescription("Retrieves key counts across all active databases (DB 0-15) for the given connection")
		.WithTags("Redis Storage")
		.Produces<GetKeyspaceInfoResponse>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status404NotFound)
		.RequireAuthorization(Policies.Authenticated);
	}
}

using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.GetServerInfo;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.Connections.GetServerInfo;

public class GetServerInfoEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("connections/{connectionId:guid}/info", async (
			IMediator sender,
			Guid connectionId,
			CancellationToken cancellationToken) =>
		{
			var query = new GetServerInfoQuery(connectionId);
			return await sender.Send<RedisServerInfoDto>(query, cancellationToken)
				.Match(Results.Ok, Results.BadRequest);
		})
		.WithName("GetServerInfo")
		.WithSummary("Get Redis server info")
		.WithDescription("Retrieves structured Redis server statistics and telemetry (INFO command)")
		.WithTags("Connections")
		.Produces<RedisServerInfoDto>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest)
		.RequireAuthorization(Policies.Authenticated);
	}
}

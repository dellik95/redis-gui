using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.GetServerConfig;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.Connections.GetServerConfig;

public class GetServerConfigEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapGet("connections/{connectionId:guid}/config", async (
			IMediator sender,
			Guid connectionId,
			string? pattern,
			CancellationToken cancellationToken) =>
		{
			var query = new GetServerConfigQuery(connectionId, pattern);
			return await sender.Send<RedisServerConfigDto>(query, cancellationToken)
				.Match(Results.Ok, Results.BadRequest);
		})
		.WithName("GetServerConfig")
		.WithSummary("Get Redis server configuration")
		.WithDescription("Retrieves Redis server configuration parameters (CONFIG GET)")
		.WithTags("Connections")
		.Produces<RedisServerConfigDto>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest)
		.RequireAuthorization(Policies.Authenticated);
	}
}

using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.UpdateServerConfig;
using RedisGUI.Domain.Redis;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.Connections.UpdateServerConfig;

public class UpdateServerConfigEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("connections/{connectionId:guid}/config", async (
			IMediator sender,
			Guid connectionId,
			UpdateServerConfigRequest request,
			CancellationToken cancellationToken) =>
		{
			var command = new UpdateServerConfigCommand(connectionId, request.ParameterName, request.ParameterValue);
			return await sender.Send<RedisSetConfigResponseDto>(command, cancellationToken)
				.Match(Results.Ok, Results.BadRequest);
		})
		.WithName("UpdateServerConfig")
		.WithSummary("Update Redis server configuration")
		.WithDescription("Updates a Redis server configuration parameter (CONFIG SET)")
		.WithTags("Connections")
		.Produces<RedisSetConfigResponseDto>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest)
		.RequireAuthorization(Policies.Authenticated);
	}
}

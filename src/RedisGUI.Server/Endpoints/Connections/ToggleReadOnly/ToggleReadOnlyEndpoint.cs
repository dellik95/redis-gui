using DispatchWeave;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.ToggleReadOnly;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using System;

namespace RedisGUI.Server.Endpoints.Connections.ToggleReadOnly;

public record ToggleReadOnlyRequest(bool IsReadOnly);

public class ToggleReadOnlyEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("connections/{connectionId:guid}/toggle-readonly", async (
			IDispatcher sender,
			Guid connectionId,
			[FromBody] ToggleReadOnlyRequest request) =>
		{
			var command = new ToggleReadOnlyCommand(connectionId, request.IsReadOnly);
			return await sender.Send(command).Match(
				isReadOnly => Results.Ok(new { ConnectionId = connectionId, IsReadOnly = isReadOnly }),
				error => error.ToProblemDetails());
		})
		.RequireAuthorization(Policies.WriteAccess)
		.WithName("ToggleReadOnly")
		.WithSummary("Toggle connection Safe Read-Only mode")
		.WithTags("Connections")
		.Produces(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest);
	}
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.UpdateConnection;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;

namespace RedisGUI.Server.Endpoints.Connections.UpdateConnection;

/// <summary>
/// Endpoint for updating existing Redis connections.
/// Handles the update of connection configurations.
/// </summary>
public class UpdateConnectionEndpoint : IEndpoint
{
	/// <summary>
	/// Maps the PUT endpoint for updating connections.
	/// </summary>
	/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
	/// <remarks>
	/// PUT /connections/{id}
	/// Updates an existing connection and returns NoContent if successful, BadRequest if update fails.
	/// </remarks>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPut("connections/{id:guid}", async (
			Guid id,
			UpdateConnectionRequest request,
			IMediator sender,
			CancellationToken cancellationToken) =>
		{
			UpdateConnectionCommand updateConnectionCommand = new(
				id,
				request.Name,
				request.Host,
				request.Port,
				request.Database,
				request.UserName,
				request.Password);

			Result result = await sender.Send(updateConnectionCommand, cancellationToken);

			return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
		})
		.WithName("UpdateConnection")
		.WithSummary("Update a Redis connection")
		.WithDescription("Updates the configuration of an existing Redis connection. Supports optimistic concurrency control.")
		.WithTags("Connections")
		.Produces(StatusCodes.Status204NoContent)
		.Produces(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status404NotFound)
		.Produces(StatusCodes.Status409Conflict)
		.RequireAuthorization(Policies.WriteAccess);
	}
}

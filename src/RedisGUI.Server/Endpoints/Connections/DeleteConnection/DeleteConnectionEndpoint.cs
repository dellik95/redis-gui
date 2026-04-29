using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.DeleteConnection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;

namespace RedisGUI.Server.Endpoints.Connections.DeleteConnection
{
	/// <summary>
	/// Endpoint for deleting existing Redis connections.
	/// Removes connection configurations from the system.
	/// </summary>
	public class DeleteConnectionEndpoint : IEndpoint
	{
		/// <summary>
		/// Maps the DELETE endpoint for removing connections.
		/// </summary>
		/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
		/// <remarks>
		/// DELETE /connections/{id}
		/// Deletes the specified connection and returns OK if successful, BadRequest if deletion fails.
		/// </remarks>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapDelete("connections/{id:guid}", async (
				IMediator sender,
				Guid id,
				CancellationToken cancellationToken) =>
			{
				DeleteConnectionCommand command = new(id);
				return await sender.Send(command, cancellationToken).Match(() => Results.Ok(), Results.BadRequest);
			})
			.WithName("DeleteConnection")
			.WithSummary("Delete a Redis connection")
			.WithDescription("Permanently deletes a Redis connection configuration. Admin only.")
			.WithTags("Connections")
			.Produces(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization(Policies.AdminOnly);
		}
	}
}

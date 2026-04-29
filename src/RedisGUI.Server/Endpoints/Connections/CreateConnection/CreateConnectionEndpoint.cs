using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.CreateConnection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;

namespace RedisGUI.Server.Endpoints.Connections.CreateConnection
{
	/// <summary>
	/// Endpoint for creating new Redis connections.
	/// Handles the creation and persistence of connection configurations.
	/// </summary>
	public class CreateConnectionEndpoint : IEndpoint
	{
		/// <summary>
		/// Maps the POST endpoint for creating new connections.
		/// </summary>
		/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
		/// <remarks>
		/// POST /connections
		/// Creates a new connection and returns the connection ID if successful, BadRequest if creation fails.
		/// </remarks>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapPost("connections", async (
				IMediator sender,
				CreateConnectionRequest request,
				CancellationToken cancellationToken) =>
			{
				CreateConnectionCommand createConnectionCommand = new(
					request.Name,
					request.Host,
					request.Port,
					request.Database,
					request.UserName,
					request.Password);

				return await sender.Send(createConnectionCommand, cancellationToken).Match(Results.Ok, Results.BadRequest);
			})
			.WithName("CreateConnection")
			.WithSummary("Create a new Redis connection")
			.WithDescription("Creates a new Redis connection configuration with the provided parameters")
			.WithTags("Connections")
			.Produces<Guid>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest)
			.RequireAuthorization(Policies.WriteAccess);
		}
	}
}

using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.CreateConnection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;
using RedisGUI.Server.Abstraction;

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
			routerBuilder.MapPost("connections", async (ISender sender, CreateConnectionRequest request) =>
				{
					var createConnectionCommand = new CreateConnectionCommand(
						request.Name,
						request.Host,
						request.Port,
						request.Database,
						request.UserName, request.Password);

					return await sender.Send<Result<Guid>>(createConnectionCommand).Match(Results.Ok, Results.BadRequest);
				})
				.WithOpenApi();
		}
	}
}

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
	/// Endpoint which represents Create connection command.
	/// </summary>
	public class CreateConnectionEndpoint : IEndpoint
	{
		/// <inheritdoc />
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

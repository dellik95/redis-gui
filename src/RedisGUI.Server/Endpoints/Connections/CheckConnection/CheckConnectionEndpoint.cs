using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.CheckConnection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.Connections.CheckConnection
{
	/// <summary>
	/// Endpoint for validating Redis connection parameters.
	/// Tests if a connection can be established with the provided configuration.
	/// </summary>
	internal sealed class CheckConnectionEndpoint: IEndpoint
	{
		/// <summary>
		/// Maps the POST endpoint for connection validation.
		/// </summary>
		/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
		/// <remarks>
		/// POST /connections/check
		/// Tests the connection parameters and returns OK if successful, BadRequest if connection fails.
		/// </remarks>
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapPost("connections/check", async (ISender sender, CheckConnectionRequest request) =>
				{
					var createConnectionCommand = new CheckConnectionCommand(
						request.Host,
						request.Port,
						request.Database,
						request.UserName, request.Password);

					return await sender.Send(createConnectionCommand)
						.Match(() => Results.Ok(), Results.BadRequest);
				})
				.WithOpenApi();
		}
	}
}

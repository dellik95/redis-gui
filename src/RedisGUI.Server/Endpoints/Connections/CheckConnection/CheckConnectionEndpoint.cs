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
	/// Endpoint which represents Check connection command.
	/// </summary>
	internal sealed class CheckConnectionEndpoint: IEndpoint
	{
		/// <inheritdoc />
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

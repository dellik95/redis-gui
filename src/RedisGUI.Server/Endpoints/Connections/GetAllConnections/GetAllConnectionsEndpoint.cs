using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.GetAllConnections;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.Connections.GetAllConnections
{
	/// <summary>
	/// Endpoint which represents retrieving all connections query.
	/// </summary>
	public class GetAllConnectionsEndpoint : IEndpoint
	{
		/// <inheritdoc />
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapGet("connections/", async (ISender sender, [AsParameters] GetAllConnectionsRequest request) =>
				{
					var query = new GetAllConnectionsQuery(request.PageNumber, request.PageSize);
					return await sender.Send(query).Match(Results.Ok, Results.BadRequest);
				})
				.WithOpenApi();
		}
	}
}

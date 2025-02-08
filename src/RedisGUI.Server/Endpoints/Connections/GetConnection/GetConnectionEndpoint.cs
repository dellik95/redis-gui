using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.GetConnectionById;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.Connections.GetConnection
{
	/// <summary>
	/// Endpoint which represents retrieving connection by id query command.
	/// </summary>
	public class GetConnectionEndpoint : IEndpoint
	{
		/// <inheritdoc />
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapGet("connections/{id:guid}", async (ISender sender, Guid id) =>
				{
					var query = new GetConnectionByIdQuery(id);
					return await sender.Send(query).Match(Results.Ok, Results.BadRequest);
				})
				.WithOpenApi();
		}
	}
}

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.GetConnectionById;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Shared.Mediator;
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
			routerBuilder.MapGet("connections/{id:guid}", async (IMediator sender, Guid id) =>
				{
					var query = new GetConnectionByIdQuery(id);
					return await sender.Send<GetConnectionResponse>(query).Match(Results.Ok, Results.BadRequest);
				})
				.WithOpenApi();
		}
	}
}

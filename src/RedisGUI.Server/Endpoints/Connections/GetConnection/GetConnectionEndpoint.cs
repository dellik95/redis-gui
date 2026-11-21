using DispatchWeave;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.GetConnectionById;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Infrastructure.Authorization;
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
			routerBuilder.MapGet("connections/{id:guid}", async (IDispatcher sender, Guid id) =>
				{
					var query = new GetConnectionByIdQuery(id);
					return await sender.Send(query).Match(Results.Ok, error => error.ToProblemDetails());
				})
				.RequireAuthorization(Policies.Authenticated)
				.WithOpenApi();
		}
	}
}

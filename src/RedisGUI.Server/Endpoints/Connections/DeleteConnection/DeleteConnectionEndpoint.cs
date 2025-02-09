using MediatR;
using Microsoft.AspNetCore.Routing;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RedisGUI.Application.Connections.DeleteConnection;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Endpoints.Connections.DeleteConnection
{
	/// <summary>
	/// Endpoint which represents delete connection command.
	/// </summary>
	public class DeleteConnectionEndpoint : IEndpoint
	{
		/// <inheritdoc />
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
		{
			routerBuilder.MapDelete("connections/{id:guid}", async (ISender sender, Guid id) =>
			{
				var command = new DeleteConnectionCommand(id);
				return await sender.Send<Result>(command).Match(() => Results.Ok(), Results.BadRequest);
			}).WithOpenApi();
		}
	}
}

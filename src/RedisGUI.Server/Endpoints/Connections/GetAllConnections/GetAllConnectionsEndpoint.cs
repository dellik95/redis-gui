using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.GetAllConnections;
using RedisGUI.Application.Connections.Shared;
using RedisGUI.Domain.Extensions;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System.Collections.Generic;

namespace RedisGUI.Server.Endpoints.Connections.GetAllConnections
{
	/// <summary>
	/// Endpoint which represents retrieving all connections query.
	/// </summary>
	public class GetAllConnectionsEndpoint : IEndpoint
	{
		/// <inheritdoc />
		public void MapEndpoint(IEndpointRouteBuilder routerBuilder) => routerBuilder.MapGet("connections/", static async (IMediator sender, [AsParameters] GetAllConnectionsRequest request) =>
																				 {
																					 GetAllConnectionsQuery query = new(request.PageNumber, request.PageSize);
																					 return await sender.Send<IEnumerable<GetConnectionResponse>>(query).Match(Results.Ok, Results.BadRequest);
																				 })
																				 .WithName("GetAllConnections")
																				 .WithSummary("Get all Redis connections")
																				 .WithDescription("Retrieves a paginated list of all Redis connection configurations")
																				 .WithTags("Connections")
																				 .Produces<IEnumerable<GetConnectionResponse>>(StatusCodes.Status200OK)
																				 .Produces(StatusCodes.Status400BadRequest)
																				 .RequireAuthorization(Policies.Authenticated);
	}
}

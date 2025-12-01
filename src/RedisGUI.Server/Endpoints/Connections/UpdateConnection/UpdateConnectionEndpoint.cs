using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.UpdateConnection;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Server.Endpoints.Connections.UpdateConnection;

/// <summary>
/// Endpoint for updating existing Redis connections.
/// Handles the update of connection configurations.
/// </summary>
public class UpdateConnectionEndpoint : IEndpoint
{
	/// <summary>
	/// Maps the PUT endpoint for updating connections.
	/// </summary>
	/// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
	/// <remarks>
	/// PUT /connections/{id}
	/// Updates an existing connection and returns NoContent if successful, BadRequest if update fails.
	/// </remarks>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPut("connections/{id:guid}", async (
			Guid id,
			UpdateConnectionRequest request,
			IMediator sender,
			CancellationToken token) =>
		{
			var updateConnectionCommand = new UpdateConnectionCommand(
				id,
				request.Name,
				request.Host,
				request.Port,
				request.Database,
				request.UserName,
				request.Password);

			var result = await sender.Send(updateConnectionCommand, token);
			
			return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
		})
		.AddOpenApiOperationTransformer((operation, context, ct) =>
		{
			operation.Summary = "Update an existing Redis connection";
			operation.Description = "Updates the configuration of an existing Redis connection";

			return Task.CompletedTask;
		});
	}
}

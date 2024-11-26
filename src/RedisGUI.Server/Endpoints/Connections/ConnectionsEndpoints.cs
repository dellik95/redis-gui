using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Connections.CheckConnection;
using RedisGUI.Application.Connections.CreateConnection;
using RedisGUI.Application.Connections.GetConnectionById;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;
using System;
using RedisGUI.Application.Connections.GetAllConnections;
using RedisGUI.Application.Connections.DeleteConnection;

namespace RedisGUI.Server.Endpoints.Connections;

/// <summary>
/// Contains endpoint definitions for Redis connection management
/// </summary>
public static class ConnectionsEndpoints
{
	/// <summary>
	/// Registers all connection-related endpoints in the HTTP request pipeline
	/// </summary>
	/// <param name="routerBuilder">The router builder to add endpoints to</param>
	/// <returns>The router builder for method chaining</returns>
	public static IEndpointRouteBuilder MapConnectionsEndpoints(this IEndpointRouteBuilder routerBuilder)
	{
		var apiVersionSet = routerBuilder.NewApiVersionSet()
			.HasApiVersion(new ApiVersion(1.0))
			.ReportApiVersions()
			.Build();

		var group = routerBuilder.MapGroup("api/v{version:apiVersion}/connections")
			.WithApiVersionSet(apiVersionSet);

		// Get a connection by ID
		group.MapGet("{id:guid}", async (ISender sender, Guid id) =>
			{
				var query = new GetConnectionByIdQuery(id);
				return await sender.Send(query).Match(Results.Ok, Results.BadRequest);
			})
			.WithOpenApi();

		// Get all connections
		group.MapGet("/", async (ISender sender, [AsParameters] GetAllConnectionsRequest request) =>
			{
				var query = new GetAllConnectionsQuery(request.PageNumber, request.PageSize);
				return await sender.Send(query).Match(Results.Ok, Results.BadRequest);
			})
			.WithOpenApi();

		// Create a new connection
		group.MapPost("/", async (ISender sender, CreateConnectionRequest request) =>
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

		// Delete connection
		group.MapDelete("{id:guid}", async (ISender sender, Guid id) =>
		{
			var command = new DeleteConnectionCommand(id);
			return await sender.Send<Result>(command).Match(() => Results.Ok(), Results.BadRequest);
		}).WithOpenApi();

		// Check connection validity
		group.MapPost("check", async (ISender sender, CheckConnectionRequest request) =>
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

		return routerBuilder;
	}
}

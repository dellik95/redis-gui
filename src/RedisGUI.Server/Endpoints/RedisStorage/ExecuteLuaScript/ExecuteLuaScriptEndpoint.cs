using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.ExecuteLuaScript;
using RedisGUI.Domain.Extensions;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Threading;

namespace RedisGUI.Server.Endpoints.RedisStorage.ExecuteLuaScript;

/// <summary>
/// Endpoint for executing Lua scripts in Redis
/// </summary>
public class ExecuteLuaScriptEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for executing Lua scripts
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("script/{connectionId:guid}/execute", async (
			IMediator sender,
			Guid connectionId,
			ExecuteLuaScriptRequest request,
			CancellationToken cancellationToken) =>
		{
			ExecuteLuaScriptCommand command = new(
				connectionId,
				request.Script,
				request.Keys ?? [],
				request.Arguments ?? []);

			return await sender.Send<ExecuteLuaScriptResponse>(command, cancellationToken)
				.Match(Results.Ok, Results.BadRequest);
		})
		.WithName("ExecuteLuaScript")
		.WithSummary("Execute a Lua script in Redis")
		.WithDescription("Executes a Lua script using EVAL with automatic SHA-based caching for performance. Use with caution as scripts can be powerful!")
		.WithTags("Redis Operations")
		.Produces<ExecuteLuaScriptResponse>(StatusCodes.Status200OK)
		.Produces(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status404NotFound)
		.RequireAuthorization(Policies.WriteAccess)
		.RequireRateLimiting("sensitive");
	}
}

/// <summary>
/// Request for executing a Lua script
/// </summary>
/// <param name="Script">The Lua script content</param>
/// <param name="Keys">Redis keys that the script will access</param>
/// <param name="Arguments">Arguments to pass to the script</param>
public sealed record ExecuteLuaScriptRequest(
	string Script,
	string[]? Keys = null,
	string[]? Arguments = null);

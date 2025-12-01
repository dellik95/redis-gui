using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.ExecuteLuaScript;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

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
			ExecuteLuaScriptRequest request) =>
		{
			var command = new ExecuteLuaScriptCommand(
				connectionId,
				request.Script,
				request.Keys ?? [],
				request.Arguments ?? []);

			return await sender.Send<ExecuteLuaScriptResponse>(command)
				.Match(Results.Ok, Results.BadRequest);
		})
		.WithName("ExecuteLuaScript")
		.WithSummary("Execute a Lua script in Redis")
		.WithDescription("Executes a Lua script using EVAL with automatic SHA-based caching");
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

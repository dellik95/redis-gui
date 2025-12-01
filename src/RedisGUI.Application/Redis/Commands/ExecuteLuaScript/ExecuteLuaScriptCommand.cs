using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.ExecuteLuaScript;

/// <summary>
/// Command to execute a Lua script in Redis.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Script">The Lua script content.</param>
/// <param name="Keys">Redis keys that the script will access.</param>
/// <param name="Arguments">Arguments to pass to the script.</param>
public sealed record ExecuteLuaScriptCommand(
	Guid ConnectionId,
	string Script,
	string[] Keys,
	string[] Arguments) : ICommand<ExecuteLuaScriptResponse>;

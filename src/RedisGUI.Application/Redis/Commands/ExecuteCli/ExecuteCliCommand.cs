using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.ExecuteCli;

/// <summary>
/// Command to execute a raw Redis CLI command
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier</param>
/// <param name="Command">The raw command string</param>
/// <param name="Database">Optional target database</param>
public sealed record ExecuteCliCommand(
    Guid ConnectionId,
    string Command,
    int? Database = null) : ICommand<ExecuteCliResponse>;

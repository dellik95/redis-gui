using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;

/// <summary>
/// Command to delete Redis keys matching a pattern.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Pattern">Pattern to match keys (e.g., "user:*", "session:*").</param>
/// <param name="Database">Optional database index.</param>
public sealed record DeleteKeysByPatternCommand(
    Guid ConnectionId,
    string Pattern,
    int? Database = null) : ICommand<DeleteKeysByPatternResponse>;

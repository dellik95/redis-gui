using System;
using System.Collections.Generic;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;

/// <summary>
/// Command to delete multiple Redis keys by their exact names.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Keys">List of keys to delete.</param>
public sealed record DeleteMultipleKeysCommand(
    Guid ConnectionId,
    IReadOnlyList<string> Keys) : ICommand<DeleteMultipleKeysResponse>;

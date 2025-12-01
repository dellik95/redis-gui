using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.DeleteRedisKey;

/// <summary>
/// Command to delete a Redis key.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Key">The key to delete.</param>
public sealed record DeleteRedisKeyCommand(Guid ConnectionId, string Key) : ICommand<bool>;

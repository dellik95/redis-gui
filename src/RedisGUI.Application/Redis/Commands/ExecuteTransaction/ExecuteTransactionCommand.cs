using System;
using System.Collections.Generic;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.ExecuteTransaction;

/// <summary>
/// Command to execute a Redis transaction (MULTI/EXEC).
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Operations">List of operations to execute atomically.</param>
public sealed record ExecuteTransactionCommand(
	Guid ConnectionId,
	List<RedisOperation> Operations) : ICommand<ExecuteTransactionResponse>;

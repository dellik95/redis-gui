using RedisGUI.Shared.Mediator;
using System;
using System.Collections.Generic;

namespace RedisGUI.Application.Redis.Commands.BulkUpdateTtl;

/// <summary>
/// Command to update TTL or persist multiple keys in bulk
/// </summary>
public sealed record BulkUpdateTtlCommand(
	Guid ConnectionId,
	IEnumerable<string> Keys,
	long? TtlSeconds,
	int? Database = null
) : ICommand<int>;

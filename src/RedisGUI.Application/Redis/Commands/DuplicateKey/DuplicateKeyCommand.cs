using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Application.Redis.Commands.DuplicateKey;

/// <summary>
/// Command to duplicate a Redis key with all its internal data and TTL intact
/// </summary>
public sealed record DuplicateKeyCommand(
	Guid ConnectionId,
	string SourceKey,
	string TargetKey,
	int? Database = null
) : ICommand;

using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Application.Redis.Commands.RenameKey;

/// <summary>
/// Command to rename a key in Redis
/// </summary>
public sealed record RenameKeyCommand(
	Guid ConnectionId,
	string OldKey,
	string NewKey,
	bool Overwrite = false,
	int? Database = null
) : ICommand;

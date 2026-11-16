using RedisGUI.Domain.Audit;
using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Connections.UpdateConnection;

/// <summary>
/// Command to update an existing Redis connection with the specified parameters.
/// </summary>
[Auditable(AuditAction.Update, "Connection")]
public sealed record UpdateConnectionCommand(
	Guid Id,
	string Name,
	string Host,
	int Port,
	int Database,
	string? UserName = null,
	string? Password = null,
	bool IsReadOnly = false) : ICommand;

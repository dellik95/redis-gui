using RedisGUI.Domain.Audit;
using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Application.Connections.ToggleReadOnly;

/// <summary>
/// Command to toggle safe read-only mode for a connection
/// </summary>
[Auditable(AuditAction.Update, "Connection")]
public sealed record ToggleReadOnlyCommand(Guid ConnectionId, bool IsReadOnly) : ICommand<bool>;

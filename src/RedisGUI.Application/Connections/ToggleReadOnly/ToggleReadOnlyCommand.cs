using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Application.Connections.ToggleReadOnly;

/// <summary>
/// Command to toggle safe read-only mode for a connection
/// </summary>
public sealed record ToggleReadOnlyCommand(Guid ConnectionId, bool IsReadOnly) : ICommand<bool>;

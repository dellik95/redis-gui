using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Connections.DeleteConnection;

/// <summary>
/// Command to delete connection by specified Id
/// </summary>
/// <param name="Id">Connection identifier</param>
public record DeleteConnectionCommand(Guid Id) : ICommand;

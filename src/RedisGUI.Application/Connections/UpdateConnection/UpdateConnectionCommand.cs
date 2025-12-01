using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Connections.UpdateConnection;

/// <summary>
/// Command to update an existing Redis connection with the specified parameters.
/// </summary>
/// <param name="Id">The unique identifier of the connection to update</param>
/// <param name="Name">The name of the connection</param>
/// <param name="Host">The Redis server host address</param>
/// <param name="Port">The Redis server port number</param>
/// <param name="Database">The Redis database number to connect to</param>
/// <param name="UserName">The username for authentication (optional)</param>
/// <param name="Password">The password for authentication (optional)</param>
public sealed record UpdateConnectionCommand(
    Guid Id,
    string Name, 
    string Host, 
    int Port, 
    int Database, 
    string UserName, 
    string Password) : ICommand;

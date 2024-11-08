using RedisGUI.Application.Abstraction.Messaging;
using System;

namespace RedisGUI.Application.Connections.CreateConnection;

/// <summary>
/// Command to create a new Redis connection with the specified parameters.
/// </summary>
/// <param name="Name">The name of the connection</param>
/// <param name="Host">The Redis server host address</param>
/// <param name="Port">The Redis server port number</param>
/// <param name="Database">The Redis database number to connect to</param>
/// <param name="UserName">The username for authentication (optional)</param>
/// <param name="Password">The password for authentication (optional)</param>
public sealed record CreateConnectionCommand(
    string Name, 
    string Host, 
    int Port, 
    int Database, 
    string UserName, 
    string Password) : ICommand<Guid>;

using RedisGUI.Application.Abstraction.Messaging;

namespace RedisGUI.Application.Connections.CheckConnection;

/// <summary>
/// Command to test a Redis connection with the specified parameters.
/// </summary>
/// <param name="Host">The Redis server host address</param>
/// <param name="Port">The Redis server port number</param>
/// <param name="Database">The Redis database number to connect to (optional)</param>
/// <param name="UserName">The username for authentication (optional)</param>
/// <param name="Password">The password for authentication (optional)</param>
public sealed record CheckConnectionCommand(
    string Host, 
    int Port, 
    int Database = 0, 
    string UserName = "", 
    string Password = "") : ICommand;

using RedisGUI.Application.Abstraction.Messaging;

namespace RedisGUI.Application.Connections.CreateConnection;

public sealed record CreateConnectionCommand(string Name, string Host, int Port, int Database, string UserName, string Password) : ICommand<Guid>;
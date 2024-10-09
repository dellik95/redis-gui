using RedisGUI.Application.Abstraction.Messaging;

namespace RedisGUI.Application.Connections.TestConnection;

public record TestConnectionCommand(string Name, string Host, int Port, int Database, string UserName, string Password) : ICommand;
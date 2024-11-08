namespace RedisGUI.Server.Endpoints.Connections;

/// <summary>
///     Represent connection request class.
/// </summary>
/// <param name="Name">Connection name</param>
/// <param name="Host">Connection host</param>
/// <param name="Port">Port</param>
/// <param name="Database">Database number</param>
/// <param name="UserName">User name</param>
/// <param name="Password">User password</param>
public record CreateConnectionRequest(
	string Name,
	string Host,
	int Port,
	int Database,
	string UserName,
	string Password);

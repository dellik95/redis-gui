namespace RedisGUI.Server.Endpoints.Connections.UpdateConnection;

/// <summary>
/// Represents an update connection request.
/// </summary>
/// <param name="Name">Connection name</param>
/// <param name="Host">Connection host</param>
/// <param name="Port">Port</param>
/// <param name="Database">Database number</param>
/// <param name="UserName">User name</param>
/// <param name="Password">User password</param>
public record UpdateConnectionRequest(
	string Name,
	string Host,
	int Port,
	int Database,
	string UserName,
	string Password);

namespace RedisGUI.Server.Endpoints.Connections;

/// <summary>
///     Represent request to check check database connection.
/// </summary>
/// <param name="Host">Connection host</param>
/// <param name="Port">Port</param>
/// <param name="Database">Database number</param>
/// <param name="UserName">User name</param>
/// <param name="Password">User password</param>
public record CheckConnectionRequest(
	string Host,
	int Port,
	int Database,
	string UserName,
	string Password);

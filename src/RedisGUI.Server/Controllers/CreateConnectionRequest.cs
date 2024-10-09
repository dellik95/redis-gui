namespace RedisGUI.Server.Controllers;

public record CreateConnectionRequest(
	string Name,
	string Host,
	int Port,
	int Database,
	string? UserName,
	string? Password);
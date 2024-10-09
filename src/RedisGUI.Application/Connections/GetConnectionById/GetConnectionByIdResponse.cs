using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;

namespace RedisGUI.Application.Connections.GetConnectionById;

public class GetConnectionByIdResponse
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Host { get; set; }
	public string Username { get; set; }
	public string Password { get; set; }
	public int Database { get; set; }

	public static GetConnectionByIdResponse FromRedisConnection(RedisConnection connection, IPasswordDecryptor passwordDecryptor)
	{
		var password = passwordDecryptor.DecryptPassword(connection.Credentials?.PasswordHash);

		return new GetConnectionByIdResponse()
		{
			Name = connection.Name.ToString(),
			Id = connection.Id,
			Host = $"{connection.Host}:{connection.Port}",
			Password = password,
			Username = connection.Credentials?.UserName,
			Database = connection.Database
		};
	}
}
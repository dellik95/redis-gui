using System;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using static RedisGUI.Domain.Connection.RedisConnection;

namespace RedisGUI.Application.Connections.Shared;

/// <summary>
/// Response object containing Redis connection details.
/// </summary>
public sealed class GetConnectionResponse
{
	/// <summary>
	/// The unique identifier of the connection.
	/// </summary>
	public Guid Id { get; private set; }

	/// <summary>
	/// The name of the connection.
	/// </summary>
	public string Name { get; private set; }

	/// <summary>
	/// The host address including port number.
	/// </summary>
	public string Host { get; private set; }

	/// <summary>
	/// The username used for authentication.
	/// </summary>
	public string Username { get; private set; }

	/// <summary>
	/// The decrypted password used for authentication.
	/// </summary>
	public string Password { get; private set; }

	/// <summary>
	/// The Redis database number.
	/// </summary>
	public int Database { get; private set; }

	/// <summary>
	/// Creates a response object from a Redis connection entity.
	/// </summary>
	/// <param name="connection">The Redis connection entity</param>
	/// <param name="passwordDecryptor">Service for password decryption</param>
	/// <returns>A new response object containing the connection details</returns>
	public static GetConnectionResponse FromRedisConnection(
		RedisConnection connection,
		IPasswordDecryptor passwordDecryptor)
	{
		var password = string.Empty;
		var userName = string.Empty;

		if (connection as RedisConnectionWithCredentials != null)
		{
			var connectionWithCredentials = (RedisConnectionWithCredentials)connection;

			password = passwordDecryptor.DecryptPassword(connectionWithCredentials.ConnectionCredentials.PasswordHash);
			userName = connectionWithCredentials.ConnectionCredentials.UserName;
		}


		return new GetConnectionResponse
		{
			Id = connection.Id,
			Name = connection.ConnectionName?.Value ?? string.Empty,
			Host = $"{connection.ServerHost}:{connection.ServerPort}",
			Username = userName,
			Password = password,
			Database = connection.DatabaseNumber,
		};
	}
}

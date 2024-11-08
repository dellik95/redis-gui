using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using System;

namespace RedisGUI.Application.Connections.GetConnectionById;

/// <summary>
/// Response object containing Redis connection details.
/// </summary>
public sealed class GetConnectionByIdResponse
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
	/// Indicates whether the connection is currently active.
	/// </summary>
	public bool IsConnected { get; private set; }

	/// <summary>
	/// Creates a response object from a Redis connection entity.
	/// </summary>
	/// <param name="connection">The Redis connection entity</param>
	/// <param name="passwordDecryptor">Service for password decryption</param>
	/// <returns>A new response object containing the connection details</returns>
	public static GetConnectionByIdResponse FromRedisConnection(
		RedisConnection connection, 
		IPasswordDecryptor passwordDecryptor)
	{
		var password = connection.ConnectionCredentials != null 
			? passwordDecryptor.DecryptPassword(connection.ConnectionCredentials.PasswordHash)
			: string.Empty;

		return new GetConnectionByIdResponse
		{
			Id = connection.Id,
			Name = connection.ConnectionName?.Value ?? string.Empty,
			Host = $"{connection.ServerHost}:{connection.ServerPort}",
			Username = connection.ConnectionCredentials?.UserName ?? string.Empty,
			Password = password,
			Database = connection.DatabaseNumber,
			IsConnected = connection.IsConnected
		};
	}
}

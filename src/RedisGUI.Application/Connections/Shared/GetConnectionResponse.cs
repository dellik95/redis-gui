using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using System;

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
	public required string Name { get; init; }

	/// <summary>
	/// The host address including port number.
	/// </summary>
	public required string Host { get; init; }

	/// <summary>
	/// The port number for the Redis connection.
	/// </summary>
	public int Port { get; private set; }

	/// <summary>
	/// The username used for authentication.
	/// </summary>
	public required string Username { get; init; }

	/// <summary>
	/// The decrypted password used for authentication.
	/// </summary>
	public required string Password { get; init; }

	/// <summary>
	/// The Redis database number.
	/// </summary>
	public int Database { get; private set; }

	/// <summary>
	/// Creates a response object from a Redis connection entity.
	/// </summary>
	/// <param name="connection">The Redis connection entity</param>
	/// <param name="passwordDecrypt">Service for password decryption</param>
	/// <returns>A new response object containing the connection details</returns>
	public static GetConnectionResponse FromRedisConnection(
		RedisConnection connection,
		IPasswordDecrypt passwordDecrypt)
	{
		string password = string.Empty;
		string userName = string.Empty;

		if (connection is RedisConnectionWithCredentials securedConnection)
		{
			password = passwordDecrypt.DecryptPassword(securedConnection.ConnectionCredentials.PasswordHash);
			userName = securedConnection.ConnectionCredentials.UserName;
		}


		return new GetConnectionResponse
		{
			Id = connection.Id,
			Name = connection.ConnectionName?.Value ?? string.Empty,
			Host = connection.ServerHost.Value,
			Port = connection.ServerPort.Value,
			Username = userName,
			Password = password,
			Database = connection.DatabaseNumber.Value,
		};
	}
}

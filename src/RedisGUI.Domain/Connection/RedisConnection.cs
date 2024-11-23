using RedisGUI.Domain.Guards;
using RedisGUI.Domain.Primitives;
using System;
using RedisGUI.Domain.Connection.Events;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents a Redis connection configuration
/// </summary>
public sealed class RedisConnection : Entity
{
	/// <summary>
	/// Creates a new instance of <see cref="RedisConnection"/>
	/// </summary>
	private RedisConnection()
	{

	}

	/// <summary>
	/// Creates a new instance of <see cref="RedisConnection"/>
	/// </summary>
	private RedisConnection(
		Guid id,
		ConnectionName connectionName,
		ConnectionHost serverHost,
		ConnectionPort serverPort,
		int databaseNumber,
		ConnectionCredentials connectionCredentials = null) : base(id)
	{
		Ensure.NotNull(connectionName, "Connection name is required", nameof(connectionName));
		Ensure.NotNull(serverHost, "Server host is required", nameof(serverHost));
		Ensure.NotNull(serverPort, "Server port is required", nameof(serverPort));

		ConnectionName = connectionName;
		ServerHost = serverHost;
		ServerPort = serverPort;
		DatabaseNumber = databaseNumber;
		ConnectionCredentials = connectionCredentials;

		RaiseDomainEvent(new ConnectionCreatedDomainEvent(id, connectionName.Value));
	}

	/// <summary>
	/// The friendly name of the connection
	/// </summary>
	public ConnectionName ConnectionName { get; private set; }

	/// <summary>
	/// The Redis server host address
	/// </summary>
	public ConnectionHost ServerHost { get; private set; }

	/// <summary>
	/// The Redis server port number
	/// </summary>
	public ConnectionPort ServerPort { get; private set; }

	/// <summary>
	/// The authentication credentials for the Redis server
	/// </summary>
	public ConnectionCredentials ConnectionCredentials { get; private set; }

	/// <summary>
	/// The Redis database number to connect to
	/// </summary>
	public int DatabaseNumber { get; private set; }

	/// <summary>
	/// Creates a new Redis connection configuration
	/// </summary>
	/// <param name="connectionName">The friendly name for this connection</param>
	/// <param name="serverHost">The Redis server host address</param>
	/// <param name="serverPort">The Redis server port number</param>
	/// <param name="databaseNumber">The Redis database number</param>
	/// <param name="connectionCredentials">Optional authentication credentials</param>
	/// <returns>A new <see cref="RedisConnection"/> instance</returns>
	public static Result<RedisConnection> Create(
		ConnectionName connectionName,
		ConnectionHost serverHost,
		ConnectionPort serverPort,
		int databaseNumber,
		ConnectionCredentials connectionCredentials = null)
	{
		try
		{
			var connection = new RedisConnection(
				Guid.NewGuid(),
				connectionName,
				serverHost,
				serverPort,
				databaseNumber,
				connectionCredentials);

			return Result.Success(connection);
		}
		catch (Exception ex)
		{
			return Result.Failure<RedisConnection>(new Error(
				"ConnectionCreationFailed",
				ex.Message));
		}
	}

	/// <summary>
	/// Updates the authentication credentials for the Redis server
	/// </summary>
	/// <param name="newCredentials">The new authentication credentials</param>
	/// <returns>A result indicating the success or failure of the update operation</returns>
	public Result UpdateCredentials(ConnectionCredentials newCredentials)
	{
		ConnectionCredentials = newCredentials;
		return Result.Success();
	}

	/// <summary>
	/// Updates the connection details for the Redis server
	/// </summary>
	/// <param name="newName">The new friendly name for the connection</param>
	/// <param name="newHost">The new Redis server host address</param>
	/// <param name="newPort">The new Redis server port number</param>
	/// <param name="newDatabaseNumber">The new Redis database number</param>
	/// <returns>A result indicating the success or failure of the update operation</returns>
	public Result UpdateConnectionDetails(
		ConnectionName newName,
		ConnectionHost newHost,
		ConnectionPort newPort,
		int newDatabaseNumber)
	{
		try
		{
			ConnectionName = newName;
			ServerHost = newHost;
			ServerPort = newPort;
			DatabaseNumber = newDatabaseNumber;

			return Result.Success();
		}
		catch (Exception ex)
		{
			return Result.Failure(new Error(
				"ConnectionUpdateFailed",
				ex.Message));
		}
	}

	/// <summary>
	/// Builds the Redis connection string from the configuration
	/// </summary>
	/// <returns>A formatted Redis connection string</returns>
	public string BuildConnectionString()
	{
		var connectionString = $"{ServerHost}:{ServerPort}";

		if (ConnectionCredentials?.UserName is not null &&
			ConnectionCredentials?.PasswordHash is not null)
		{
			connectionString = $"{ConnectionCredentials.UserName}:{ConnectionCredentials.PasswordHash}@{connectionString}";
		}

		if (DatabaseNumber > 0)
		{
			connectionString = $"{connectionString}/database={DatabaseNumber}";
		}

		return connectionString;
	}
}

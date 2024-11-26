using System;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents redis connection configuration with credentials
/// </summary>
public sealed class RedisConnectionWithCredentials : RedisConnection
{
	/// <inheritdoc />
	private RedisConnectionWithCredentials() : base()
	{

	}


	/// <inheritdoc />
	public RedisConnectionWithCredentials(
		Guid id,
		ConnectionName connectionName,
		ConnectionHost serverHost,
		ConnectionPort serverPort,
		int databaseNumber,
		ConnectionCredentials connectionCredentials) : base(id, connectionName, serverHost, serverPort, databaseNumber)
	{
		this.ConnectionCredentials = connectionCredentials;
	}

	/// <summary>
	/// The authentication credentials for the Redis server
	/// </summary>
	public ConnectionCredentials ConnectionCredentials { get; private set; }


	/// <inheritdoc />
	protected override string GetBaseConnectionString() => $"{ConnectionCredentials.UserName}:{ConnectionCredentials.PasswordHash}@{ServerHost}:{ServerPort}";
}

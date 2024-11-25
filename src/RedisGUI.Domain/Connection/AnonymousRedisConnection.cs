using System;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents anonymous redis connection configuration
/// </summary>
public sealed class AnonymousRedisConnection : RedisConnection
{
	/// <inheritdoc />
	private AnonymousRedisConnection() : base()
	{

	}

	/// <inheritdoc />
	public AnonymousRedisConnection(
		Guid id,
		ConnectionName connectionName,
		ConnectionHost serverHost,
		ConnectionPort serverPort,
		int databaseNumber) : base(id, connectionName, serverHost, serverPort, databaseNumber)
	{
	}

	/// <inheritdoc />
	protected override string GetBaseConnectionString() => $"{ServerHost}:{ServerPort}";
}

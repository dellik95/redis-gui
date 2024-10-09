using RedisGUI.Domain.Guards;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Connection
{
	public sealed class RedisConnection : Entity
	{

		private RedisConnection()
		{
			
		}

		private RedisConnection(Guid id, ConnectionName name, ConnectionHost host, ConnectionPort port, ConnectionCredentials credentials, int database) : base(id)
		{
			Ensure.NotNull(name, "The name is required.", nameof(name));
			Ensure.NotNull(name, "The host is required.", nameof(name));
			Ensure.NotNull(host, "The port is required.", nameof(port));

			Name = name;
			Host = host;
			Port = port;
			Database = database;
			Credentials = credentials;
		}

		public ConnectionName Name { get; init; }
		public ConnectionHost Host { get; init; }

		public ConnectionPort Port { get; init; }

		public ConnectionCredentials Credentials { get; init; }

		public int Database { get; init; }

		public static RedisConnection Create(
			ConnectionName name,
			ConnectionHost host,
			ConnectionPort port,
			int database,
			ConnectionCredentials credentials = null)
		{
			var connection = new RedisConnection(Guid.NewGuid(), name, host, port, credentials, database);
			return connection;
		}
	}
}
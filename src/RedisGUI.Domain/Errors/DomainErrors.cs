using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Errors;

/// <summary>
/// Contains domain error
/// </summary>
public static class DomainErrors
{
	/// <summary>
	/// Storage errors
	/// </summary>
	public static class Storage
	{
		/// <summary>
		/// Item not found in storage
		/// </summary>
		public static Error ItemNotFound => new("Storage.NotFound", "The item(s) not found.");
	}

	/// <summary>
	/// Connection errors
	/// </summary>
	public static class Connection
	{
		/// <summary>
		/// Connection not found
		/// </summary>
		public static Error ConnectionNotFound => new("Connection.NotFound", "The connection not found.");

		/// <summary>
		/// Not established connection
		/// </summary>
		public static Error ConnectionNotEstablished => new("Connection.NotEstablished", "The connection can not be established.");
	}

	/// <summary>
	/// Connection name errors
	/// </summary>
	public static class ConnectionName
	{
		/// <summary>
		/// Longer than error
		/// </summary>
		public static Error LongerThanAllowed => new("Name.LongerThanAllowed", "The connection name is longer than allowed.");
	}

	/// <summary>
	/// Connection host name
	/// </summary>
	public static class ConnectionHost
	{
		/// <summary>
		/// Longer than error
		/// </summary>
		public static Error LongerThanAllowed => new("Value.LongerThanAllowed", "The host is longer than allowed.");

		/// <summary>
		/// Invalid format error
		/// </summary>
		public static Error InvalidFormat => new("Value.InvalidFormat", "The host has invalid format.");
	}

	/// <summary>
	/// Connection port errors
	/// </summary>
	public static class ConnectionPort
	{
		/// <summary>
		/// Less than error
		/// </summary>
		public static Error LessThanAllowed => new("Port.InvalidNumber", "The port has invalid value. Port should be greater than 0.");
	}

	/// <summary>
	/// Connection credentials error
	/// </summary>
	public class ConnectionCredentials
	{
		/// <summary>
		/// Invalid user credentials error
		/// </summary>
		public static Error UsernameAndPasswordInvalid => new("Credentials.UsernameAndPasswordInvalid", "Username and Password should be specified.");
	}
}

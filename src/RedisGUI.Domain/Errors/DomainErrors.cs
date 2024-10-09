using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Errors
{
	public static class DomainErrors
	{
		public static class Connection
		{
			public static Error ConnectionNotFound => new("Connection.NotFound", "The connection not found.");

			public static Error ConnectionNotEstablished => new("Connection.otEstablished", "The connection can not be established.");
		}

		public static class ConnectionName
		{
			public static Error LongerThanAllowed => new("Name.LongerThanAllowed", "The connection name is longer than allowed.");
		}		
		public static class ConnectionHost
		{
			public static Error LongerThanAllowed => new("Value.LongerThanAllowed", "The host is longer than allowed.");

			public static Error InvalidFormat => new("Value.InvalidFormat", "The host has invalid format.");
		}

		public static class ConnectionPort
		{
			public static Error LessThanAllowed => new("Port.InvalidNumber", "The port has invalid value. Port should be greater than 0.");
		}

		public class ConnectionCredentials
		{
			public static Error UserNameAndPasswordInvalid => new("Credentials.ameAndPasswordInvalid", "UserName and Password should be specified.");

		}
	}
}

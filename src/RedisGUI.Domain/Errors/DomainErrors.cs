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
		public static DomainError ItemNotFound => new("Storage.NotFound", "The item(s) not found.");

		/// <summary>
		/// Concurrency conflict occurred during update
		/// </summary>
		public static DomainError ConcurrencyConflict => new("Storage.ConcurrencyConflict", "The entity has been modified by another user. Please refresh and try again.");
	}

	/// <summary>
	/// Connection errors
	/// </summary>
	public static class Connection
	{
		/// <summary>
		/// Connection not found
		/// </summary>
		public static DomainError ConnectionNotFound => new("Connection.NotFound", "The connection not found.");

		/// <summary>
		/// Connection not found by ID
		/// </summary>
		public static DomainError NotFound => new("Connection.NotFound", "The connection with the specified ID was not found.");

		/// <summary>
		/// Not established connection
		/// </summary>
		public static DomainError ConnectionNotEstablished => new("Connection.NotEstablished", "The connection can not be established.");

		/// <summary>
		/// Connection creation failed
		/// </summary>
		public static DomainError CreationFailed => new("Connection.CreationFailed", "Failed to create the connection.");

		/// <summary>
		/// Connection update failed
		/// </summary>
		public static DomainError UpdateFailed => new("Connection.UpdateFailed", "Failed to update the connection.");

		/// <summary>
		/// Invalid connection configuration
		/// </summary>
		public static DomainError InvalidConfiguration => new("Connection.InvalidConfiguration", "The connection configuration is invalid.");

		public static DomainError ReadOnlyMode => new("Connection.ReadOnlyMode", "Connection is in Safe Mode (Read-Only). Write operations are not allowed.");
	}

	/// <summary>
	/// Connection name errors
	/// </summary>
	public static class ConnectionName
	{
		/// <summary>
		/// Longer than error
		/// </summary>
		public static DomainError LongerThanAllowed => new("Name.LongerThanAllowed", "The connection name is longer than allowed.");
	}

	/// <summary>
	/// Connection host name
	/// </summary>
	public static class ConnectionHost
	{
		/// <summary>
		/// Longer than error
		/// </summary>
		public static DomainError LongerThanAllowed => new("Value.LongerThanAllowed", "The host is longer than allowed.");

		/// <summary>
		/// Invalid format error
		/// </summary>
		public static DomainError InvalidFormat => new("Value.InvalidFormat", "The host has invalid format.");
	}

	/// <summary>
	/// Connection port errors
	/// </summary>
	public static class ConnectionPort
	{
		/// <summary>
		/// Less than error
		/// </summary>
		public static DomainError LessThanAllowed => new("Port.InvalidNumber", "The port has invalid value. Port should be greater than 0.");
	}

	/// <summary>
	/// Database number errors
	/// </summary>
	public static class DatabaseNumber
	{
		/// <summary>
		/// Less than allowed error
		/// </summary>
		public static DomainError LessThanAllowed => new("DatabaseNumber.LessThanAllowed", "The database number must be greater than or equal to 0.");

		/// <summary>
		/// Greater than allowed error
		/// </summary>
		public static DomainError GreaterThanAllowed => new("DatabaseNumber.GreaterThanAllowed", "The database number exceeds the maximum allowed value.");
	}

	/// <summary>
	/// Connection owner ID errors
	/// </summary>
	public static class ConnectionOwnerId
	{
		/// <summary>
		/// Empty owner ID error
		/// </summary>
		public static DomainError Empty => new("ConnectionOwnerId.Empty", "Owner ID cannot be empty.");
	}

	/// <summary>
	/// Connection credentials error
	/// </summary>
	public class ConnectionCredentials
	{
		/// <summary>
		/// Invalid user credentials error
		/// </summary>
		public static DomainError UsernameAndPasswordInvalid => new("Credentials.UsernameAndPasswordInvalid", "Username and Password should be specified.");
	}

	/// <summary>
	/// Email errors
	/// </summary>
	public static class Email
	{
		/// <summary>
		/// Email is empty
		/// </summary>
		public static DomainError Empty => new("Email.Empty", "Email address is required.");

		/// <summary>
		/// Email has invalid format
		/// </summary>
		public static DomainError InvalidFormat => new("Email.InvalidFormat", "Email address has an invalid format.");

		/// <summary>
		/// Email already exists
		/// </summary>
		public static DomainError AlreadyExists => new("Email.AlreadyExists", "Email address is already registered.");
	}

	/// <summary>
	/// Password errors
	/// </summary>
	public static class Password
	{
		/// <summary>
		/// Password is empty
		/// </summary>
		public static DomainError Empty => new("Password.Empty", "Password is required.");

		/// <summary>
		/// Password is too short
		/// </summary>
		public static DomainError TooShort => new("Password.TooShort", "Password must be at least 8 characters long.");

		/// <summary>
		/// Password is too long
		/// </summary>
		public static DomainError TooLong => new("Password.TooLong", "Password must not exceed 128 characters.");
	}

	/// <summary>
	/// User profile errors
	/// </summary>
	public static class UserProfile
	{
		/// <summary>
		/// First name is required
		/// </summary>
		public static DomainError FirstNameRequired => new("UserProfile.FirstNameRequired", "First name is required.");

		/// <summary>
		/// Last name is required
		/// </summary>
		public static DomainError LastNameRequired => new("UserProfile.LastNameRequired", "Last name is required.");

		/// <summary>
		/// First name is too long
		/// </summary>
		public static DomainError FirstNameTooLong => new("UserProfile.FirstNameTooLong", "First name must not exceed 100 characters.");

		/// <summary>
		/// Last name is too long
		/// </summary>
		public static DomainError LastNameTooLong => new("UserProfile.LastNameTooLong", "Last name must not exceed 100 characters.");
	}

	/// <summary>
	/// User errors
	/// </summary>
	public static class User
	{
		/// <summary>
		/// User not found
		/// </summary>
		public static DomainError NotFound => new("User.NotFound", "User was not found.");

		/// <summary>
		/// Invalid credentials
		/// </summary>
		public static DomainError InvalidCredentials => new("User.InvalidCredentials", "Invalid email or password.");

		/// <summary>
		/// User is deactivated
		/// </summary>
		public static DomainError Deactivated => new("User.Deactivated", "User account is deactivated.");

		/// <summary>
		/// User already deactivated
		/// </summary>
		public static DomainError AlreadyDeactivated => new("User.AlreadyDeactivated", "User account is already deactivated.");

		/// <summary>
		/// Invalid password
		/// </summary>
		public static DomainError InvalidPassword => new("User.InvalidPassword", "Current password is incorrect.");

		/// <summary>
		/// Email already exists
		/// </summary>
		public static DomainError EmailAlreadyExists => new("User.EmailAlreadyExists", "An account with this email already exists.");

		/// <summary>
		/// User is not authenticated
		/// </summary>
		public static DomainError Unauthorized => new("User.Unauthorized", "Authentication is required to perform this operation.");
	}

	/// <summary>
	/// Redis value errors
	/// </summary>
	public static class RedisValue
	{
		/// <summary>
		/// Redis key is empty
		/// </summary>
		public static DomainError EmptyKey => new("RedisValue.EmptyKey", "Redis key cannot be empty.");

		/// <summary>
		/// Redis type is empty
		/// </summary>
		public static DomainError EmptyType => new("RedisValue.EmptyType", "Redis type cannot be empty.");

		/// <summary>
		/// Invalid value data for the specified type
		/// </summary>
		public static DomainError InvalidData => new("RedisValue.InvalidData", "Invalid value data for the specified type.");
	}

	/// <summary>
	/// Redis operation errors
	/// </summary>
	public static class Redis
	{
		/// <summary>
		/// Redis key not found
		/// </summary>
		public static DomainError KeyNotFound => new("Redis.KeyNotFound", "The specified key was not found.");

		/// <summary>
		/// Failed to delete keys by pattern
		/// </summary>
		public static DomainError DeleteByPatternFailed => new("Redis.DeleteByPatternFailed", "Failed to delete keys by pattern.");

		/// <summary>
		/// Invalid Redis type
		/// </summary>
		public static DomainError InvalidType => new("Redis.InvalidType", "Invalid Redis type.");

		/// <summary>
		/// Invalid value for Redis type
		/// </summary>
		public static DomainError InvalidValue => new("Redis.InvalidValue", "Invalid value for the specified Redis type.");

		/// <summary>
		/// Unsupported Redis type
		/// </summary>
		public static DomainError UnsupportedType => new("Redis.UnsupportedType", "Unsupported Redis type.");

		/// <summary>
		/// Failed to update Redis value
		/// </summary>
		public static DomainError UpdateFailed => new("Redis.UpdateFailed", "Failed to update value.");

		/// <summary>
		/// Failed to publish message
		/// </summary>
		public static DomainError PublishFailed => new("Redis.PublishFailed", "Failed to publish message.");

		/// <summary>
		/// Failed to subscribe to channel
		/// </summary>
		public static DomainError SubscribeFailed => new("Redis.SubscribeFailed", "Failed to subscribe to channel.");

		/// <summary>
		/// Subscription not found
		/// </summary>
		public static DomainError SubscriptionNotFound => new("Redis.SubscriptionNotFound", "Subscription not found.");

		/// <summary>
		/// Failed to unsubscribe from channel
		/// </summary>
		public static DomainError UnsubscribeFailed => new("Redis.UnsubscribeFailed", "Failed to unsubscribe from channel.");

		/// <summary>
		/// Transaction failed
		/// </summary>
		public static DomainError TransactionFailed => new("Redis.TransactionFailed", "Transaction failed.");

		/// <summary>
		/// Script execution failed
		/// </summary>
		public static DomainError ScriptFailed => new("Redis.ScriptFailed", "Script execution failed.");

		/// <summary>
		/// Unsupported Redis key type
		/// </summary>
		public static DomainError UnsupportedKeyType => new("Redis.UnsupportedKeyType", "Unsupported Redis key type.");

		/// <summary>
		/// Invalid value type for Redis operation
		/// </summary>
		public static DomainError InvalidValueType => new("Redis.InvalidValueType", "Invalid value type for Redis operation.");

		/// <summary>
		/// Failed to retrieve value from Redis
		/// </summary>
		public static DomainError ValueRetrievalFailed => new("Redis.ValueRetrievalFailed", "Failed to retrieve value from Redis.");

		/// <summary>
		/// Failed to update value in Redis
		/// </summary>
		public static DomainError ValueUpdateFailed => new("Redis.ValueUpdateFailed", "Failed to update value in Redis.");
	}

	/// <summary>
	/// Audit log errors
	/// </summary>
	public static class AuditLog
	{
		/// <summary>
		/// Audit log not found
		/// </summary>
		public static DomainError NotFound => new("AuditLog.NotFound", "Audit log not found.");
	}

	/// <summary>
	/// Health check errors
	/// </summary>
	public static class Health
	{
		/// <summary>
		/// Health information not found
		/// </summary>
		public static DomainError NotFound => new("Health.NotFound", "No health information available for this connection.");
	}

	/// <summary>
	/// Import/Export errors
	/// </summary>
	public static class Import
	{
		/// <summary>
		/// Failed to parse import data
		/// </summary>
		public static DomainError ParseError => new("Import.ParseError", "Failed to parse import data.");
	}

	/// <summary>
	/// Connection manager errors
	/// </summary>
	public static class ConnectionManager
	{
		/// <summary>
		/// Operation was cancelled
		/// </summary>
		public static DomainError Cancelled => new("ConnectionManager.Cancelled", "Operation was cancelled.");

		/// <summary>
		/// An unexpected error occurred
		/// </summary>
		public static DomainError UnexpectedError => new("ConnectionManager.UnexpectedError", "An unexpected error occurred.");
	}

	/// <summary>
	/// Metrics collection errors
	/// </summary>
	public static class MetricsCollection
	{
		/// <summary>
		/// Exception during metrics collection
		/// </summary>
		public static DomainError Exception => new("MetricsCollection.Exception", "An exception occurred during metrics collection.");
	}

	/// <summary>
	/// Create Error for Invalid value.
	/// </summary>
	/// <param name="errorMessage"></param>
	/// <returns></returns>
	public static DomainError InvalidValue(string errorMessage) => new("Value.Invalid", errorMessage);
}

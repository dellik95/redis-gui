using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Connection
{
	public class ConnectionCredentials
	{
		public string UserName { get; init; }

		public string PasswordHash { get; init; }

		private ConnectionCredentials()
		{
			
		}

		private ConnectionCredentials(string username, string passwordHash)
		{
			UserName = username;
			PasswordHash = passwordHash;
		}

		public static Result<ConnectionCredentials> Create(string username, string password, IPasswordEncryptor passwordEncryptor)
		{
			if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
			{
				return Result.Success<ConnectionCredentials>(null);
			}

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
			{
				return Result.Failure<ConnectionCredentials>(DomainErrors.ConnectionCredentials.UserNameAndPasswordInvalid);
			}

			var passwordHash = passwordEncryptor.EncryptPassword(password);

			var credentials = new ConnectionCredentials(username, passwordHash);
			return credentials;
		}
	}
}

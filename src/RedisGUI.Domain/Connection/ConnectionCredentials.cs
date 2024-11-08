using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;


namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represent class with user credentials <see cref="ConnectionCredentials"/>
/// </summary>
public class ConnectionCredentials
{
	/// <summary>
	/// Create new instance of <see cref="ConnectionCredentials"/>
	/// </summary>
	private ConnectionCredentials()
	{
		PasswordHash = string.Empty;
		UserName = string.Empty;
	}

	/// <summary>
	/// Create new instance of <see cref="ConnectionCredentials"/>
	/// </summary>
	/// <param name="username">User name</param>
	/// <param name="passwordHash">Password hash</param>
	private ConnectionCredentials(string username, string passwordHash)
	{
		UserName = username;
		PasswordHash = passwordHash;
	}

	/// <summary>
	/// User name
	/// </summary>
	public string UserName { get; init; }

	/// <summary>
	/// Password hash
	/// </summary>
	public string PasswordHash { get; init; }

	/// <summary>
	/// Create new instance of <see cref="ConnectionCredentials"/>
	/// </summary>
	/// <param name="username">User name</param>
	/// <param name="password">User password</param>
	/// <param name="passwordEncryptor">Password encryptor</param>
	/// <returns></returns>
	public static Result<ConnectionCredentials> Create(string username, string password, IPasswordEncryptor passwordEncryptor)
	{
		if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
		{
			return Result.Success(new ConnectionCredentials());
		}

		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
		{
			return Result.Failure<ConnectionCredentials>(DomainErrors.ConnectionCredentials.UsernameAndPasswordInvalid);
		}

		var passwordHash = passwordEncryptor.EncryptPassword(password);

		var credentials = new ConnectionCredentials(username, passwordHash);
		return credentials!;
	}
}

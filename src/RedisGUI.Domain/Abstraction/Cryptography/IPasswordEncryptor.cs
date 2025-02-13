namespace RedisGUI.Domain.Abstraction.Cryptography;

/// <summary>
///     Represents the password hasher interface
/// </summary>
public interface IPasswordEncryptor
{
	/// <summary>
	///     Encrypt input string
	/// </summary>
	/// <param name="value">String value to encrypt</param>
	/// <returns>The encrypted string value</returns>
	string EncryptPassword(string value);
}

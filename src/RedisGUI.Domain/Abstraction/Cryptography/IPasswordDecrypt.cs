namespace RedisGUI.Domain.Abstraction.Cryptography;

/// <summary>
///     Password decrypt contact
/// </summary>
public interface IPasswordDecrypt
{
	/// <summary>
	///     Decrypt value.
	/// </summary>
	/// <param name="encryptedValue">Encrypted value to decrypt</param>
	/// <returns>The decrypted string value</returns>
	string DecryptPassword(string encryptedValue);
}

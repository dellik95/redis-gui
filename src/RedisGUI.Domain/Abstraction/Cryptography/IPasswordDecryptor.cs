namespace RedisGUI.Domain.Abstraction.Cryptography;

public interface IPasswordDecryptor
{
	/// <summary>
	/// Decrypt value.
	/// </summary>
	/// <param name="encryptedValue">Encrypted value to decrypt.</param>
	/// <returns></returns>
	string DecryptPassword(string encryptedValue);
}
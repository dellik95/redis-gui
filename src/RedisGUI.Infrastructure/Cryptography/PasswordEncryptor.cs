using RedisGUI.Domain.Abstraction.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace RedisGUI.Infrastructure.Cryptography;

/// <summary>
///     Represents the password encryption/decryption
/// </summary>
internal sealed class PasswordEncryptor : IPasswordEncryptor, IPasswordDecrypt
{
	private readonly int saltSize = 16;
	private readonly int keySize = 32;
	private readonly int ivSize = 16;
	private readonly int iterations = 10000;

	private readonly CryptographyConfiguration config;

	/// <summary>
	/// Creates a new instance of PasswordEncryptor
	/// </summary>
	/// <param name="options"></param>
	public PasswordEncryptor(IOptions<CryptographyConfiguration> options)
	{
		config = options.Value;
	}

	/// <inheritdoc />
	public string DecryptPassword(string encryptedValue)
	{
		if (string.IsNullOrWhiteSpace(encryptedValue))
		{
			return string.Empty;
		}

		byte[] cipherTextBytesWithSalt = Convert.FromBase64String(encryptedValue);

		byte[] salt = new byte[saltSize];
		Buffer.BlockCopy(cipherTextBytesWithSalt, 0, salt, 0, saltSize);

		(byte[]? aesKey, byte[]? aesIv) = GenerateKeyAndIv(config.SecurityKey, salt);

		byte[] encryptedBytes = new byte[cipherTextBytesWithSalt.Length - saltSize];
		Buffer.BlockCopy(cipherTextBytesWithSalt, saltSize, encryptedBytes, 0, encryptedBytes.Length);

		byte[] decryptedBytes;

		using (Aes aesAlg = Aes.Create())
		{
			aesAlg.Key = aesKey;
			aesAlg.IV = aesIv;

			using ICryptoTransform decrypt = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
			decryptedBytes = decrypt.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
		}

		return Encoding.UTF8.GetString(decryptedBytes);
	}

	/// <inheritdoc />
	public string EncryptPassword(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		byte[] salt = GenerateSalt();
		(byte[]? aesKey, byte[]? aesIv) = GenerateKeyAndIv(config.SecurityKey, salt);
		byte[] encryptedBytes;

		using (Aes aesAlg = Aes.Create())
		{
			aesAlg.Key = aesKey;
			aesAlg.IV = aesIv;

			using ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
			byte[] plainTextBytes = Encoding.UTF8.GetBytes(value);
			encryptedBytes = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);
		}

		byte[] result = new byte[saltSize + encryptedBytes.Length];
		Buffer.BlockCopy(salt, 0, result, 0, saltSize);
		Buffer.BlockCopy(encryptedBytes, 0, result, saltSize, encryptedBytes.Length);

		return Convert.ToBase64String(result);
	}


	private byte[] GenerateSalt()
	{
		byte[] salt = new byte[saltSize];
		using RandomNumberGenerator rng = RandomNumberGenerator.Create();
		rng.GetBytes(salt);
		return salt;
	}

	private (byte[] Key, byte[] IV) GenerateKeyAndIv(string key, byte[] salt)
	{
		byte[] aesKey = Rfc2898DeriveBytes.Pbkdf2(key, salt, iterations, HashAlgorithmName.SHA256, keySize);
		byte[] aesIv = Rfc2898DeriveBytes.Pbkdf2(key, salt, iterations, HashAlgorithmName.SHA256, ivSize);
		return (aesKey, aesIv);
	}
}

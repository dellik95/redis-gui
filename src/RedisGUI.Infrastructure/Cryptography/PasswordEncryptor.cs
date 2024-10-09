using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RedisGUI.Domain.Abstraction.Cryptography;

namespace RedisGUI.Infrastructure.Cryptography;

/// <summary>
/// Represents the password encryption/decryption
/// </summary>
internal sealed class PasswordEncryptor : IPasswordEncryptor, IPasswordDecryptor
{

	private const int SaltSize = 16;
	private const int KeySize = 32;
	private const int IvSize = 16;
	private const int Iterations = 10000;

	private readonly CryptographyConfiguration _config;

	public PasswordEncryptor(IOptions<CryptographyConfiguration> options)
	{
		this._config = options.Value;
	}

	/// <inheritdoc />
	public string EncryptPassword(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		var salt = GenerateSalt();
		var (aesKey, aesIv) = GenerateKeyAndIv(_config.SecurityKey, salt);
		byte[] encryptedBytes;

		using (var aesAlg = Aes.Create())
		{
			aesAlg.Key = aesKey;
			aesAlg.IV = aesIv;

			using (var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
			{
				var plainTextBytes = Encoding.UTF8.GetBytes(value);
				encryptedBytes = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);
			}
		}

		var result = new byte[SaltSize + encryptedBytes.Length];
		Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
		Buffer.BlockCopy(encryptedBytes, 0, result, SaltSize, encryptedBytes.Length);

		return Convert.ToBase64String(result);
	}

	/// <inheritdoc />
	public string DecryptPassword(string encryptedValue)
	{
		if (string.IsNullOrWhiteSpace(encryptedValue))
			return null;

		var cipherTextBytesWithSalt = Convert.FromBase64String(encryptedValue);

		var salt = new byte[SaltSize];
		Buffer.BlockCopy(cipherTextBytesWithSalt, 0, salt, 0, SaltSize);

		var (aesKey, aesIv) = GenerateKeyAndIv(_config.SecurityKey, salt);

		var encryptedBytes = new byte[cipherTextBytesWithSalt.Length - SaltSize];
		Buffer.BlockCopy(cipherTextBytesWithSalt, SaltSize, encryptedBytes, 0, encryptedBytes.Length);

		byte[] decryptedBytes;

		using (var aesAlg = Aes.Create())
		{
			aesAlg.Key = aesKey;
			aesAlg.IV = aesIv;

			using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
			{
				decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
			}
		}

		return Encoding.UTF8.GetString(decryptedBytes);
	}


	private static byte[] GenerateSalt()
	{
		var salt = new byte[SaltSize];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(salt);
		return salt;
	}

	private static (byte[] Key, byte[] IV) GenerateKeyAndIv(string key, byte[] salt)
	{
		using var keyDerivationFunction = new Rfc2898DeriveBytes(key, salt, Iterations, HashAlgorithmName.SHA256);
		var aesKey = keyDerivationFunction.GetBytes(KeySize);
		var aesIv = keyDerivationFunction.GetBytes(IvSize);
		return (aesKey, aesIv);
	}
}
using Microsoft.Extensions.Options;
using RedisGUI.Domain.Abstraction.Cryptography;
using System;
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
			return null;
		}

		var cipherTextBytesWithSalt = Convert.FromBase64String(encryptedValue);

		var salt = new byte[saltSize];
		Buffer.BlockCopy(cipherTextBytesWithSalt, 0, salt, 0, saltSize);

		var (aesKey, aesIv) = GenerateKeyAndIv(config.SecurityKey, salt);

		var encryptedBytes = new byte[cipherTextBytesWithSalt.Length - saltSize];
		Buffer.BlockCopy(cipherTextBytesWithSalt, saltSize, encryptedBytes, 0, encryptedBytes.Length);

		byte[] decryptedBytes;

		using (var aesAlg = Aes.Create())
		{
			aesAlg.Key = aesKey;
			aesAlg.IV = aesIv;

			using (var decrypt = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
			{
				decryptedBytes = decrypt.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
			}
		}

		return Encoding.UTF8.GetString(decryptedBytes);
	}

	/// <inheritdoc />
	public string EncryptPassword(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var salt = GenerateSalt();
		var (aesKey, aesIv) = GenerateKeyAndIv(config.SecurityKey, salt);
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

		var result = new byte[saltSize + encryptedBytes.Length];
		Buffer.BlockCopy(salt, 0, result, 0, saltSize);
		Buffer.BlockCopy(encryptedBytes, 0, result, saltSize, encryptedBytes.Length);

		return Convert.ToBase64String(result);
	}


	private byte[] GenerateSalt()
	{
		var salt = new byte[saltSize];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(salt);
		return salt;
	}

	private (byte[] Key, byte[] IV) GenerateKeyAndIv(string key, byte[] salt)
	{
		using var keyDerivationFunction = new Rfc2898DeriveBytes(key, salt, iterations, HashAlgorithmName.SHA256);
		var aesKey = keyDerivationFunction.GetBytes(keySize);
		var aesIv = keyDerivationFunction.GetBytes(ivSize);
		return (aesKey, aesIv);
	}
}

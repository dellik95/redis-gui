using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using RedisGUI.Infrastructure.Cryptography;
using System;

namespace RedisGUI.Infrastructure.Tests.Cryptography;

/// <summary>
/// Tests for PasswordEncryptor
/// </summary>
[TestFixture]
public class PasswordEncryptorTests
{
	private IOptions<CryptographyConfiguration> _options = null!;
	private PasswordEncryptor _encryptor = null!;

	[SetUp]
	public void Setup()
	{
		var config = new CryptographyConfiguration
		{
			SecurityKey = "TestSecurityKey123456789012345678901234567890"
		};
		_options = Options.Create(config);
		_encryptor = new PasswordEncryptor(_options);
	}

	[Test]
	public void EncryptPassword_WithValidValue_ReturnsEncryptedString()
	{
		// Arrange
		var password = "MySecretPassword123";

		// Act
		var encrypted = _encryptor.EncryptPassword(password);

		// Assert
		encrypted.Should().NotBeNullOrWhiteSpace();
		encrypted.Should().NotBe(password);
		encrypted.Should().NotContain(password);
	}

	[Test]
	public void EncryptPassword_WithEmptyString_ReturnsEmptyString()
	{
		// Arrange
		var password = string.Empty;

		// Act
		var encrypted = _encryptor.EncryptPassword(password);

		// Assert
		encrypted.Should().BeEmpty();
	}

	[Test]
	public void EncryptPassword_WithWhitespace_ReturnsEmptyString()
	{
		// Arrange
		var password = "   ";

		// Act
		var encrypted = _encryptor.EncryptPassword(password);

		// Assert
		encrypted.Should().BeEmpty();
	}

	[Test]
	public void EncryptPassword_WithNull_ReturnsEmptyString()
	{
		// Arrange
		string? password = null;

		// Act
		var encrypted = _encryptor.EncryptPassword(password!);

		// Assert
		encrypted.Should().BeEmpty();
	}

	[Test]
	public void EncryptPassword_SamePasswordTwice_ProducesDifferentEncryptedValues()
	{
		// Arrange
		var password = "MySecretPassword123";

		// Act
		var encrypted1 = _encryptor.EncryptPassword(password);
		var encrypted2 = _encryptor.EncryptPassword(password);

		// Assert
		encrypted1.Should().NotBe(encrypted2); // Different salt should produce different encrypted values
	}

	[Test]
	public void DecryptPassword_WithEncryptedValue_ReturnsOriginalValue()
	{
		// Arrange
		var originalPassword = "MySecretPassword123";
		var encrypted = _encryptor.EncryptPassword(originalPassword);

		// Act
		var decrypted = _encryptor.DecryptPassword(encrypted);

		// Assert
		decrypted.Should().Be(originalPassword);
	}

	[Test]
	public void DecryptPassword_WithEmptyString_ReturnsEmptyString()
	{
		// Arrange
		var encrypted = string.Empty;

		// Act
		var decrypted = _encryptor.DecryptPassword(encrypted);

		// Assert
		decrypted.Should().BeEmpty();
	}

	[Test]
	public void DecryptPassword_WithWhitespace_ReturnsEmptyString()
	{
		// Arrange
		var encrypted = "   ";

		// Act
		var decrypted = _encryptor.DecryptPassword(encrypted);

		// Assert
		decrypted.Should().BeEmpty();
	}

	[Test]
	public void DecryptPassword_WithNull_ReturnsEmptyString()
	{
		// Arrange
		string? encrypted = null;

		// Act
		var decrypted = _encryptor.DecryptPassword(encrypted!);

		// Assert
		decrypted.Should().BeEmpty();
	}

	[Test]
	public void EncryptAndDecrypt_MultiplePasswords_WorksCorrectly()
	{
		// Arrange
		var passwords = new[]
		{
			"Password1",
			"AnotherPassword123",
			"SpecialChars!@#$%^&*()",
			"Unicode测试密码",
			"VeryLongPasswordThatExceedsNormalLength12345678901234567890"
		};

		foreach (var password in passwords)
		{
			// Act
			var encrypted = _encryptor.EncryptPassword(password);
			var decrypted = _encryptor.DecryptPassword(encrypted);

			// Assert
			decrypted.Should().Be(password, $"Failed for password: {password}");
		}
	}

	[Test]
	public void DecryptPassword_WithInvalidBase64_ThrowsException()
	{
		// Arrange
		var invalidEncrypted = "NotAValidBase64String!!!";

		// Act & Assert
		Assert.Throws<FormatException>(() => _encryptor.DecryptPassword(invalidEncrypted));
	}

	[Test]
	public void DecryptPassword_WithDifferentSecurityKey_ProducesDifferentResult()
	{
		// Arrange
		var password = "MySecretPassword123";
		var encrypted = _encryptor.EncryptPassword(password);

		// Create a new encryptor with a different security key
		var differentConfig = new CryptographyConfiguration
		{
			SecurityKey = "DifferentSecurityKey123456789012345678901234567890"
		};
		var differentOptions = Options.Create(differentConfig);
		var differentEncryptor = new PasswordEncryptor(differentOptions);

		// Act
		var decrypted = differentEncryptor.DecryptPassword(encrypted);

		// Assert
		decrypted.Should().NotBe(password); // Should not decrypt correctly with different key
	}

	[Test]
	public void EncryptPassword_WithSpecialCharacters_EncryptsAndDecryptsCorrectly()
	{
		// Arrange
		var password = "P@ssw0rd!@#$%^&*()_+-=[]{}|;':\",./<>?";

		// Act
		var encrypted = _encryptor.EncryptPassword(password);
		var decrypted = _encryptor.DecryptPassword(encrypted);

		// Assert
		decrypted.Should().Be(password);
	}

	[Test]
	public void EncryptPassword_WithUnicodeCharacters_EncryptsAndDecryptsCorrectly()
	{
		// Arrange
		var password = "密码测试🔐";

		// Act
		var encrypted = _encryptor.EncryptPassword(password);
		var decrypted = _encryptor.DecryptPassword(encrypted);

		// Assert
		decrypted.Should().Be(password);
	}
}


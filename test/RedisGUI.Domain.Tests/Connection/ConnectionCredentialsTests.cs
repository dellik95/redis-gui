using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Tests for ConnectionCredentials value object
/// </summary>
public class ConnectionCredentialsTests
{
	[Test]
	public void Create_BothEmpty_ShouldReturnNull()
	{
		// Arrange
		MockPasswordEncryptor encryptor = new();

		// Act
		Result<ConnectionCredentials?> result = ConnectionCredentials.Create("", "", encryptor);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeNull();
	}

	[Test]
	public void Create_ValidCredentials_ShouldCreateInstance()
	{
		// Arrange
		MockPasswordEncryptor encryptor = new();

		// Act
		Result<ConnectionCredentials?> result = ConnectionCredentials.Create("username", "password", encryptor);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.UserName.Should().Be("username");
		result.Value.PasswordHash.Should().Be("encrypted_password");
	}

	[Test]
	public void Create_OnlyUsername_ShouldReturnFailure()
	{
		// Arrange
		MockPasswordEncryptor encryptor = new();

		// Act
		Result<ConnectionCredentials?> result = ConnectionCredentials.Create("username", "", encryptor);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.ConnectionCredentials.UsernameAndPasswordInvalid);
	}

	[Test]
	public void Create_OnlyPassword_ShouldReturnFailure()
	{
		// Arrange
		MockPasswordEncryptor encryptor = new();

		// Act
		Result<ConnectionCredentials?> result = ConnectionCredentials.Create("", "password", encryptor);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.ConnectionCredentials.UsernameAndPasswordInvalid);
	}

	private class MockPasswordEncryptor : IPasswordEncryptor
	{
		public string EncryptPassword(string password) => $"encrypted_{password}";
	}
}


using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Tests.Users;

/// <summary>
/// Tests for PasswordHash value object
/// </summary>
[TestFixture]
public class PasswordHashTests
{
	[Test]
	public void Create_ValidPassword_ShouldCreateInstance()
	{
		// Arrange
		MockPasswordHasher hasher = new();

		// Act
		Result<PasswordHash> result = PasswordHash.Create("ValidPassword123!", hasher);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Value.Should().Be("hashed_ValidPassword123!");
	}

	[Test]
	public void Create_EmptyPassword_ShouldReturnFailure()
	{
		// Arrange
		MockPasswordHasher hasher = new();

		// Act
		Result<PasswordHash> result = PasswordHash.Create("", hasher);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Password.Empty);
	}

	[Test]
	public void Create_WhitespacePassword_ShouldReturnFailure()
	{
		// Arrange
		MockPasswordHasher hasher = new();

		// Act
		Result<PasswordHash> result = PasswordHash.Create("   ", hasher);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Password.Empty);
	}

	[Test]
	public void Create_PasswordTooShort_ShouldReturnFailure()
	{
		// Arrange
		MockPasswordHasher hasher = new();

		// Act
		Result<PasswordHash> result = PasswordHash.Create("Short1!", hasher);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Password.TooShort);
	}

	[Test]
	public void Create_PasswordTooLong_ShouldReturnFailure()
	{
		// Arrange
		MockPasswordHasher hasher = new();
		string longPassword = new('a', 129);

		// Act
		Result<PasswordHash> result = PasswordHash.Create(longPassword, hasher);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Password.TooLong);
	}

	[Test]
	public void FromHash_ValidHash_ShouldCreateInstance()
	{
		// Arrange & Act
		PasswordHash passwordHash = PasswordHash.FromHash("existing_hash");

		// Assert
		passwordHash.Value.Should().Be("existing_hash");
	}

	[Test]
	public void Equals_SameValue_ShouldReturnTrue()
	{
		// Arrange
		PasswordHash passwordHash1 = PasswordHash.FromHash("hash");
		PasswordHash passwordHash2 = PasswordHash.FromHash("hash");

		// Act & Assert
		passwordHash1.Should().Be(passwordHash2);
		passwordHash1.GetHashCode().Should().Be(passwordHash2.GetHashCode());
	}

	[Test]
	public void Equals_DifferentValue_ShouldReturnFalse()
	{
		// Arrange
		PasswordHash passwordHash1 = PasswordHash.FromHash("hash1");
		PasswordHash passwordHash2 = PasswordHash.FromHash("hash2");

		// Act & Assert
		passwordHash1.Should().NotBe(passwordHash2);
	}

	private class MockPasswordHasher : IPasswordHasher
	{
		public string Hash(string password) => $"hashed_{password}";

		public bool Verify(string password, string hashedPassword)
		{
			throw new System.NotImplementedException();
		}
	}
}


using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Tests.Users;

/// <summary>
/// Tests for Email value object
/// </summary>
[TestFixture]
public class EmailTests
{
	[Test]
	public void Create_ValidEmail_ShouldCreateInstance()
	{
		// Arrange & Act
		Result<Email> result = Email.Create("test@example.com");

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Value.Should().Be("test@example.com");
	}

	[Test]
	public void Create_ValidEmail_ShouldLowercase()
	{
		// Arrange & Act
		Result<Email> result = Email.Create("TEST@EXAMPLE.COM");

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Value.Should().Be("test@example.com");
	}

	[Test]
	public void Create_EmptyEmail_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<Email> result = Email.Create("");

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Email.Empty);
	}

	[Test]
	public void Create_WhitespaceEmail_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<Email> result = Email.Create("   ");

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Email.Empty);
	}

	[Test]
	public void Create_InvalidEmail_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<Email> result = Email.Create("invalid-email");

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Email.InvalidFormat);
	}

	[Test]
	public void Create_EmailWithoutAt_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<Email> result = Email.Create("invalidemail.com");

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Email.InvalidFormat);
	}

	[Test]
	public void Equals_SameValue_ShouldReturnTrue()
	{
		// Arrange
		Email email1 = Email.Create("test@example.com").Value!;
		Email email2 = Email.Create("test@example.com").Value!;

		// Act & Assert
		email1.Should().Be(email2);
		email1.GetHashCode().Should().Be(email2.GetHashCode());
	}

	[Test]
	public void Equals_DifferentValue_ShouldReturnFalse()
	{
		// Arrange
		Email email1 = Email.Create("test1@example.com").Value!;
		Email email2 = Email.Create("test2@example.com").Value!;

		// Act & Assert
		email1.Should().NotBe(email2);
	}
}


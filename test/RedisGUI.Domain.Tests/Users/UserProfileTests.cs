using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Tests.Users;

/// <summary>
/// Tests for UserProfile value object
/// </summary>
[TestFixture]
public class UserProfileTests
{
	[Test]
	public void Create_ValidNames_ShouldCreateInstance()
	{
		// Arrange & Act
		Result<UserProfile> result = UserProfile.Create("John", "Doe");

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.FirstName.Should().Be("John");
		result.Value.LastName.Should().Be("Doe");
		result.Value.FullName.Should().Be("John Doe");
	}

	[Test]
	public void Create_NamesWithWhitespace_ShouldTrim()
	{
		// Arrange & Act
		Result<UserProfile> result = UserProfile.Create("  John  ", "  Doe  ");

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.FirstName.Should().Be("John");
		result.Value.LastName.Should().Be("Doe");
	}

	[Test]
	public void Create_EmptyFirstName_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<UserProfile> result = UserProfile.Create("", "Doe");

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.UserProfile.FirstNameRequired);
	}

	[Test]
	public void Create_EmptyLastName_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<UserProfile> result = UserProfile.Create("John", "");

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.UserProfile.LastNameRequired);
	}

	[Test]
	public void Create_FirstNameTooLong_ShouldReturnFailure()
	{
		// Arrange
		string longFirstName = new('a', 101);

		// Act
		Result<UserProfile> result = UserProfile.Create(longFirstName, "Doe");

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.UserProfile.FirstNameTooLong);
	}

	[Test]
	public void Create_LastNameTooLong_ShouldReturnFailure()
	{
		// Arrange
		string longLastName = new('a', 101);

		// Act
		Result<UserProfile> result = UserProfile.Create("John", longLastName);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.UserProfile.LastNameTooLong);
	}

	[Test]
	public void Equals_SameValues_ShouldReturnTrue()
	{
		// Arrange
		UserProfile profile1 = UserProfile.Create("John", "Doe").Value!;
		UserProfile profile2 = UserProfile.Create("John", "Doe").Value!;

		// Act & Assert
		profile1.Should().Be(profile2);
		profile1.GetHashCode().Should().Be(profile2.GetHashCode());
	}

	[Test]
	public void Equals_DifferentValues_ShouldReturnFalse()
	{
		// Arrange
		UserProfile profile1 = UserProfile.Create("John", "Doe").Value!;
		UserProfile profile2 = UserProfile.Create("Jane", "Smith").Value!;

		// Act & Assert
		profile1.Should().NotBe(profile2);
	}
}


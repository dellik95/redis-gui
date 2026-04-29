using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Primitives;
using System;

namespace RedisGUI.Domain.Tests.Users;

/// <summary>
/// Tests for User aggregate root
/// </summary>
[TestFixture]
public class UserTests
{
	[Test]
	[Ignore("Method not yet implemented")]
	public void Create_ValidUser_ShouldCreateInstance()
	{
		// Arrange
		Email email = Email.Create("test@example.com").Value!;
		PasswordHash passwordHash = PasswordHash.Create("password", new MockPasswordHasher()).Value!;
		UserProfile profile = UserProfile.Create("John", "Doe").Value!;

		// Act
		Result<User> result = User.Create(email, passwordHash, profile, UserRole.User);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Email.Should().Be(email);
		result.Value.Profile.Should().Be(profile);
		result.Value.Role.Should().Be(UserRole.User);
		result.Value.IsActive.Should().BeTrue();
	}

	[Test]
	[Ignore("Method not yet implemented")]
	public void UpdateProfile_ValidProfile_ShouldUpdate()
	{
		// Arrange
		User user = CreateTestUser();
		UserProfile newProfile = UserProfile.Create("Jane", "Smith").Value!;

		// Act
		Result result = user.UpdateProfile(newProfile);

		// Assert
		result.IsSuccess.Should().BeTrue();
		user.Profile.Should().Be(newProfile);
	}

	[Test]
	[Ignore("Method not yet implemented")]
	public void ChangePassword_ValidOldPassword_ShouldUpdate()
	{
		// Arrange
		User user = CreateTestUser();
		MockPasswordHasher hasher = new();
		PasswordHash newPasswordHash = PasswordHash.Create("newpassword", hasher).Value!;

		// Act
		Result result = user.ChangePassword("password", newPasswordHash, hasher);

		// Assert
		result.IsSuccess.Should().BeTrue();
		user.PasswordHash.Should().Be(newPasswordHash);
	}

	[Test]
	[Ignore("Method not yet implemented")]
	public void ChangePassword_InvalidOldPassword_ShouldReturnFailure()
	{
		// Arrange
		User user = CreateTestUser();
		MockPasswordHasher hasher = new();
		PasswordHash newPasswordHash = PasswordHash.Create("newpassword", hasher).Value!;

		// Act
		Result result = user.ChangePassword("wrongpassword", newPasswordHash, hasher);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.User.InvalidPassword);
	}

	[Test]
	[Ignore("Method not yet implemented")]
	public void RecordLogin_ShouldUpdateLastLoginAt()
	{
		// Arrange
		User user = CreateTestUser();
		DateTime? beforeLogin = user.LastLoginAt;

		// Act
		user.RecordLogin();

		// Assert
		user.LastLoginAt.Should().NotBeNull();
		user.LastLoginAt.Should().BeAfter(beforeLogin ?? DateTime.MinValue);
	}

	[Test]
	[Ignore("Method not yet implemented")]
	public void Deactivate_ActiveUser_ShouldDeactivate()
	{
		// Arrange
		User user = CreateTestUser();
		user.IsActive.Should().BeTrue();

		// Act
		Result result = user.Deactivate();

		// Assert
		result.IsSuccess.Should().BeTrue();
		user.IsActive.Should().BeFalse();
	}

	[Test]
	[Ignore("Method not yet implemented")]
	public void Deactivate_AlreadyDeactivated_ShouldReturnFailure()
	{
		// Arrange
		User user = CreateTestUser();
		user.Deactivate();

		// Act
		Result result = user.Deactivate();

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.User.AlreadyDeactivated);
	}

	[Test]
	[Ignore("Method not yet implemented")]
	public void Activate_DeactivatedUser_ShouldActivate()
	{
		// Arrange
		User user = CreateTestUser();
		user.Deactivate();

		// Act
		Result result = user.Activate();

		// Assert
		result.IsSuccess.Should().BeTrue();
		user.IsActive.Should().BeTrue();
	}

	[Test]
	[Ignore("Method not yet implemented")]
	public void ChangeRole_ShouldUpdateRole()
	{
		// Arrange
		User user = CreateTestUser();

		// Act
		Result result = user.ChangeRole(UserRole.Admin);

		// Assert
		result.IsSuccess.Should().BeTrue();
		user.Role.Should().Be(UserRole.Admin);
	}

	private static User CreateTestUser()
	{
		Email email = Email.Create("test@example.com").Value!;
		PasswordHash passwordHash = PasswordHash.Create("password", new MockPasswordHasher()).Value!;
		UserProfile profile = UserProfile.Create("John", "Doe").Value!;
		return User.Create(email, passwordHash, profile, UserRole.User).Value!;
	}

	private class MockPasswordHasher : IPasswordHasher
	{
		public string Hash(string password)
		{
			throw new NotImplementedException();
		}

		public static string HashPassword(string password) => $"hashed_{password}";
		public bool Verify(string password, string hash) => hash == $"hashed_{password}";
	}
}


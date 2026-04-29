using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Users.Events;

namespace RedisGUI.Domain.Users;

/// <summary>
/// User aggregate root representing a system user
/// </summary>
public class User : Entity, IAggregateRoot
{
	private User() { } // EF Core

	private User(
		System.Guid id,
		Email email,
		PasswordHash passwordHash,
		UserProfile profile,
		UserRole role) : base(id)
	{
		Email = email;
		PasswordHash = passwordHash;
		Profile = profile;
		Role = role;
		IsActive = true;
		CreatedAt = System.DateTime.UtcNow;

		RaiseDomainEvent(new UserRegisteredDomainEvent(id, email.Value));
	}

	/// <summary>
	/// Gets the user's email address
	/// </summary>
	public Email Email { get; private set; } = null!;

	/// <summary>
	/// Gets the user's password hash
	/// </summary>
	public PasswordHash PasswordHash { get; private set; } = null!;

	/// <summary>
	/// Gets the user's profile information
	/// </summary>
	public UserProfile Profile { get; private set; } = null!;

	/// <summary>
	/// Gets the user's role
	/// </summary>
	public UserRole Role { get; private set; }

	/// <summary>
	/// Gets whether the user account is active
	/// </summary>
	public bool IsActive { get; private set; }

	/// <summary>
	/// Gets the date and time when the user was created
	/// </summary>
	public System.DateTime CreatedAt { get; private set; }

	/// <summary>
	/// Gets the date and time of the user's last login
	/// </summary>
	public System.DateTime? LastLoginAt { get; private set; }

	/// <summary>
	/// Creates a new user
	/// </summary>
	/// <param name="email">The user's email address</param>
	/// <param name="passwordHash">The user's password hash</param>
	/// <param name="profile">The user's profile information</param>
	/// <param name="role">The user's role (defaults to User)</param>
	/// <returns>Result containing the new user or an error</returns>
	public static Result<User> Create(
		Email email,
		PasswordHash passwordHash,
		UserProfile profile,
		UserRole role = UserRole.User)
	{
		User user = new(System.Guid.NewGuid(), email, passwordHash, profile, role);
		return Result.Success(user);
	}

	/// <summary>
	/// Updates the user's profile information
	/// </summary>
	/// <param name="newProfile">The new profile information</param>
	/// <returns>Result indicating success or failure</returns>
	public Result UpdateProfile(UserProfile newProfile)
	{
		Profile = newProfile;
		return Result.Success();
	}

	/// <summary>
	/// Changes the user's password
	/// </summary>
	/// <param name="oldPassword">The current password</param>
	/// <param name="newPassword">The new password</param>
	/// <param name="hasher">The password hasher service</param>
	/// <returns>Result indicating success or failure</returns>
	public Result ChangePassword(string oldPassword, PasswordHash newPassword, IPasswordHasher hasher)
	{
		if (!hasher.Verify(oldPassword, PasswordHash.Value))
			return Result.Failure(DomainErrors.User.InvalidPassword);

		PasswordHash = newPassword;
		RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id));
		return Result.Success();
	}

	/// <summary>
	/// Records a user login
	/// </summary>
	public void RecordLogin()
	{
		LastLoginAt = System.DateTime.UtcNow;
		RaiseDomainEvent(new UserLoggedInDomainEvent(Id, LastLoginAt.Value));
	}

	/// <summary>
	/// Deactivates the user account
	/// </summary>
	/// <returns>Result indicating success or failure</returns>
	public Result Deactivate()
	{
		if (!IsActive)
			return Result.Failure(DomainErrors.User.AlreadyDeactivated);

		IsActive = false;
		RaiseDomainEvent(new UserDeactivatedDomainEvent(Id));
		return Result.Success();
	}

	/// <summary>
	/// Activates the user account
	/// </summary>
	/// <returns>Result indicating success or failure</returns>
	public Result Activate()
	{
		if (IsActive)
			return Result.Success(); // Already active

		IsActive = true;
		return Result.Success();
	}

	/// <summary>
	/// Changes the user's role
	/// </summary>
	/// <param name="newRole">The new role</param>
	/// <returns>Result indicating success or failure</returns>
	public Result ChangeRole(UserRole newRole)
	{
		Role = newRole;
		return Result.Success();
	}
}

using RedisGUI.Domain.Errors;

namespace RedisGUI.Domain.Users;

/// <summary>
/// User profile value object containing user's personal information
/// </summary>
public sealed class UserProfile : ValueObject
{
	/// <summary>
	/// Gets the user's first name
	/// </summary>
	public string FirstName { get; }

	/// <summary>
	/// Gets the user's last name
	/// </summary>
	public string LastName { get; }

	/// <summary>
	/// Gets the user's full name
	/// </summary>
	public string FullName => $"{FirstName} {LastName}";

	private UserProfile(string firstName, string lastName)
	{
		FirstName = firstName;
		LastName = lastName;
	}

	/// <summary>
	/// Creates a new user profile
	/// </summary>
	/// <param name="firstName">The user's first name</param>
	/// <param name="lastName">The user's last name</param>
	/// <returns>Result containing the user profile or an error</returns>
	public static Result<UserProfile> Create(string firstName, string lastName)
	{
		if (string.IsNullOrWhiteSpace(firstName))
			return Result.Failure<UserProfile>(DomainErrors.UserProfile.FirstNameRequired);

		if (string.IsNullOrWhiteSpace(lastName))
			return Result.Failure<UserProfile>(DomainErrors.UserProfile.LastNameRequired);

		if (firstName.Length > 100)
			return Result.Failure<UserProfile>(DomainErrors.UserProfile.FirstNameTooLong);

		if (lastName.Length > 100)
			return Result.Failure<UserProfile>(DomainErrors.UserProfile.LastNameTooLong);

		return Result.Success(new UserProfile(firstName.Trim(), lastName.Trim()));
	}

	protected override IEnumerable<object> GetAtomicValues()
	{
		yield return FirstName;
		yield return LastName;
	}
}

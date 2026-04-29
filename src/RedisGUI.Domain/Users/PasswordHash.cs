using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Errors;

namespace RedisGUI.Domain.Users;

/// <summary>
/// Password hash value object representing a hashed password
/// </summary>
public sealed class PasswordHash : ValueObject
{
	/// <summary>
	/// Gets the hashed password value
	/// </summary>
	public string Value { get; }

	private PasswordHash(string value) => Value = value;

	/// <summary>
	/// Creates a password hash from a plain text password
	/// </summary>
	/// <param name="password">The plain text password</param>
	/// <param name="hasher">The password hasher service</param>
	/// <returns>Result containing the password hash or an error</returns>
	public static Result<PasswordHash> Create(string password, IPasswordHasher hasher)
	{
		if (string.IsNullOrWhiteSpace(password))
			return Result.Failure<PasswordHash>(DomainErrors.Password.Empty);

		if (password.Length < 8)
			return Result.Failure<PasswordHash>(DomainErrors.Password.TooShort);

		if (password.Length > 128)
			return Result.Failure<PasswordHash>(DomainErrors.Password.TooLong);

		string hashedPassword = hasher.Hash(password);
		return Result.Success(new PasswordHash(hashedPassword));
	}

	/// <summary>
	/// Creates a password hash from an already hashed value (for loading from database)
	/// </summary>
	/// <param name="hashedValue">The already hashed password</param>
	/// <returns>Password hash instance</returns>
	public static PasswordHash FromHash(string hashedValue)
	{
		return new PasswordHash(hashedValue);
	}

	protected override IEnumerable<object> GetAtomicValues()
	{
		yield return Value;
	}
}

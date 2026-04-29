using RedisGUI.Domain.Errors;
using System.Net.Mail;

namespace RedisGUI.Domain.Users;

/// <summary>
/// Email value object representing a valid email address
/// </summary>
public sealed class Email : ValueObject
{
	/// <summary>
	/// Gets the email address value
	/// </summary>
	public string Value { get; }

	private Email(string value) => Value = value;

	/// <summary>
	/// Creates a new email instance
	/// </summary>
	/// <param name="value">The email address string</param>
	/// <returns>Result containing the email or an error</returns>
	public static Result<Email> Create(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return Result.Failure<Email>(DomainErrors.Email.Empty);

		if (!IsValidEmail(value))
			return Result.Failure<Email>(DomainErrors.Email.InvalidFormat);

		return Result.Success(new Email(value.ToLowerInvariant()));
	}

	private static bool IsValidEmail(string email)
	{
		try
		{
			MailAddress addr = new(email);
			return addr.Address == email;
		}
		catch
		{
			return false;
		}
	}

	protected override IEnumerable<object> GetAtomicValues()
	{
		yield return Value;
	}
}

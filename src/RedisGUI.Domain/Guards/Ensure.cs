using RedisGUI.Domain.Exceptions;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Guards;

public class Ensure
{
	public static void NotEmpty(string value, string message, string argumentName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException(message, argumentName);
		}
	}

	public static void NotEmpty(string value, Error error)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ErrorException(error);
		}
	}


	public static void LessOrEqualZero(int value, string message, string argumentName)
	{
		if (value <= 0)
		{
			throw new ArgumentException(message, argumentName);
		}
	}

	public static void NotNull<T>(T value, string message, string argumentName)
	{
		if (value is null)
		{
			throw new ArgumentException(message, argumentName);
		}
	}

	public static void IsNot(Func<bool> action, string message, string argumentName)
	{
		if (action())
		{
			throw new ArgumentException(message, argumentName);
		}
	}

	public static void IsNot(Func<bool> action, Error error)
	{
		if (action())
		{
			throw new ErrorException(error);
		}
	}
}
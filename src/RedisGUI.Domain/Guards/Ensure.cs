using RedisGUI.Domain.Exceptions;
using RedisGUI.Domain.Primitives;
using System;

namespace RedisGUI.Domain.Guards;

/// <summary>
/// Static helper class providing common validation and guard checks for method parameters and business rules
/// </summary>
public static class Ensure
{
	/// <summary>
	/// Check if input string is not empty
	/// </summary>
	/// <param name="value">Input string</param>
	/// <param name="message">Error message</param>
	/// <param name="argumentName">Argument name</param>
	/// <exception cref="ArgumentException">Exception</exception>
	public static void NotEmpty(string value, string message, string argumentName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException(message, argumentName);
		}
	}

	/// <summary>
	/// Check if input string is not empty
	/// </summary>
	/// <param name="value">Input string</param>
	/// <param name="error">Error</param>
	/// <exception cref="ArgumentException">Exception</exception>
	public static void NotEmpty(string value, Error error)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ErrorException(error);
		}
	}

	/// <summary>
	/// Check if input number is greater than zero
	/// </summary>
	/// <param name="value">Input number</param>
	/// <param name="message">Error message</param>
	/// <param name="argumentName">Argument name</param>
	/// <exception cref="ArgumentException">Exception</exception>
	public static void LessOrEqualZero(int value, string message, string argumentName)
	{
		if (value <= 0)
		{
			throw new ArgumentException(message, argumentName);
		}
	}

	/// <summary>
	/// Check if input value is not null
	/// </summary>
	/// <param name="value">Input number</param>
	/// <param name="message">Error message</param>
	/// <param name="argumentName">Argument name</param>
	/// <exception cref="ArgumentException">Exception</exception>
	public static void NotNull<T>(T value, string message, string argumentName)
	{
		if (value is null)
		{
			throw new ArgumentException(message, argumentName);
		}
	}

	/// <summary>
	/// Check if action returns true
	/// </summary>
	/// <param name="action">Condition</param>
	/// <param name="error">Error</param>
	/// <exception cref="ErrorException">Exception</exception>
	public static void Is(Func<bool> action, Error error)
	{
		if (!action())
		{
			throw new ErrorException(error);
		}
	}
}

using System;

namespace RedisGUI.Domain.Primitives;

/// <summary>
/// Represents an error with a code and message
/// </summary>
/// <param name="Code">The error code</param>
/// <param name="Message">The error message</param>
public record Error(string Code, string Message)
{
	/// <summary>
	/// Represents a null or empty error
	/// </summary>
	public static Error None = new(string.Empty, string.Empty);

	/// <summary>
	/// Represents an error when a null value is provided
	/// </summary>
	public static Error NullValue = new("Error.NullValue", "Null value was provided");


	public static implicit operator Error(string message) => new Error("Error.General", message);

	public static implicit operator Error(Exception ex) => new Error("Error.General", ex.Message);
}

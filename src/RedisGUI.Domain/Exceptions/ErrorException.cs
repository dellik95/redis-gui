using RedisGUI.Domain.Extensions;

namespace RedisGUI.Domain.Exceptions;

/// <summary>
/// Represents an exception that wraps one or more domain errors
/// </summary>
public class ErrorException : Exception
{
	private readonly List<Error> errors = [];

	/// <summary>
	/// Gets the collection of errors associated with this exception
	/// </summary>
	public IReadOnlyList<Error> Errors => errors.AsReadOnly();

	/// <summary>
	/// Gets the primary error associated with this exception
	/// </summary>
	public Error Error => errors.Count > 0 ? errors[0] : Error.None;

	/// <summary>
	/// Creates a new instance of ErrorException with a single error
	/// </summary>
	/// <param name="error">The error that caused this exception</param>
	public ErrorException(Error error) : base(error.Message)
	{
		errors.Add(error);
	}

	/// <summary>
	/// Creates a new instance of ErrorException with multiple errors
	/// </summary>
	/// <param name="errors">The collection of errors that caused this exception</param>
	public ErrorException(params Error[] errors) : base(errors.Length > 0 ? errors[0].Message : "An error occurred")
	{
		this.errors = [.. errors];
	}

	/// <summary>
	/// Returns a formatted string representation of all errors in this exception
	/// </summary>
	/// <returns>A multi-line string containing all error codes and messages</returns>
	public override string ToString()
	{
		return errors.ToFormattedMessage();
	}
}


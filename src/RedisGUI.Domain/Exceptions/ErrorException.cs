using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;
using System;
using System.Collections.Generic;

namespace RedisGUI.Domain.Exceptions;

/// <summary>
/// Represents an exception that wraps one or more domain errors
/// </summary>
public class ErrorException : Exception
{
	private readonly List<Error> errors = [];

	/// <summary>
	/// Creates a new instance of ErrorException with a single error
	/// </summary>
	/// <param name="error">The error that caused this exception</param>
	public ErrorException(Error error) : base($"An error occurred: {error.Code}")
	{
		errors.Add(error);
	}

	/// <summary>
	/// Creates a new instance of ErrorException with multiple errors
	/// </summary>
	/// <param name="errors">The collection of errors that caused this exception</param>
	public ErrorException(params Error[] errors) : base($"An error occurred: {errors.ToCodeString()}")
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

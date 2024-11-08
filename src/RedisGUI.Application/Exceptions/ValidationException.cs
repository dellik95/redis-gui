using System;
using System.Collections.Generic;

namespace RedisGUI.Application.Exceptions;

/// <summary>
/// Exception thrown when validation errors occur in the application.
/// </summary>
public class ValidationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ValidationException"/> class.
	/// </summary>
	/// <param name="errors">Collection of validation errors that caused this exception.</param>
	public ValidationException(IEnumerable<ValidationError> errors)
	{
		Errors = errors;
	}

	/// <summary>
	/// Gets the collection of validation errors associated with this exception.
	/// </summary>
	public IEnumerable<ValidationError> Errors { get; init; }
}

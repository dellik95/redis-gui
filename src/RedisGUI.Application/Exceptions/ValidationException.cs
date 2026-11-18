using System;
using System.Collections.Generic;
using System.Linq;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Exceptions;

/// <summary>
/// Exception thrown when validation errors occur in the application.
/// </summary>
public class ValidationException : Exception, IResultErrorException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ValidationException"/> class.
	/// </summary>
	/// <param name="errors">Collection of validation errors that caused this exception.</param>
	public ValidationException(IEnumerable<ValidationError> errors) : base("One or more validation errors occurred.")
	{
		Errors = errors;
	}

	/// <summary>
	/// Gets the collection of validation errors associated with this exception.
	/// </summary>
	public IEnumerable<ValidationError> Errors { get; init; }

	public Error Error => new ValidationFailureError(Errors.GroupBy(error => error.PropertyName)
		.ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
}

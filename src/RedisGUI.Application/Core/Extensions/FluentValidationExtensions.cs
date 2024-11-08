using FluentValidation;
using RedisGUI.Domain.Primitives;
using System;

namespace RedisGUI.Application.Core.Extensions;

/// <summary>
/// Extension methods for FluentValidation rule builders
/// </summary>
public static class FluentValidationExtensions
{
	/// <summary>
	/// Adds a custom error to the validation rule
	/// </summary>
	/// <typeparam name="T">The type being validated</typeparam>
	/// <typeparam name="TProperty">The property type being validated</typeparam>
	/// <param name="builder">The rule builder instance</param>
	/// <param name="error">The error to be applied</param>
	/// <returns>The rule builder for method chaining</returns>
	/// <exception cref="ArgumentNullException">Thrown when error is null</exception>
	public static IRuleBuilder<T, TProperty> WithError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> builder,
			Error error)
	{
		if (error is null)
		{
			throw new ArgumentNullException(nameof(error), "Could not be null.");
		}

		return builder.WithErrorCode(error.Code).WithMessage(error.Message);
	}
}

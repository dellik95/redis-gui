using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace RedisGUI.Server.Extensions;

/// <summary>
/// Extension methods for adding validation to minimal API endpoints
/// </summary>
public static class ValidationExtensions
{
	/// <summary>
	/// Adds FluentValidation validation to a route handler
	/// </summary>
	/// <typeparam name="T">The type to validate</typeparam>
	/// <param name="builder">The route handler builder</param>
	/// <returns>The route handler builder for chaining</returns>
	public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
	{
		return builder.AddEndpointFilter(async (context, next) =>
		{
			IValidator<T>? validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();

			if (validator is null)
			{
				// No validator registered, proceed
				return await next(context);
			}

			// Find the model argument
			T? model = default;
			foreach (object? argument in context.Arguments)
			{
				if (argument is T typedModel)
				{
					model = typedModel;
					break;
				}
			}

			if (model is null)
			{
				return Results.BadRequest(new { Error = "Request model is required" });
			}

			ValidationResult validationResult = await validator.ValidateAsync(model, context.HttpContext.RequestAborted);

			if (!validationResult.IsValid)
			{
				Dictionary<string, string[]> errors = validationResult.Errors
					.GroupBy(e => e.PropertyName)
					.ToDictionary(
						g => g.Key,
						g => g.Select(e => e.ErrorMessage).ToArray());

				return Results.ValidationProblem(errors);
			}

			return await next(context);
		});
	}
}


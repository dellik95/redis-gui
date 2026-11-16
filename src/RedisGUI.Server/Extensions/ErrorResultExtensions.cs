using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace RedisGUI.Server.Extensions;

/// <summary>
/// Extension methods for converting domain errors to HTTP results
/// </summary>
public static class ErrorResultExtensions
{
	/// <summary>
	/// Converts an Error to an IResult with appropriate status code and structured response
	/// </summary>
	public static IResult ToProblemDetails(this Error error, HttpContext? httpContext = null)
	{
		if (error is ValidationFailureError validation)
		{
			return ValidationError(new Dictionary<string, string[]>(validation.Errors), httpContext);
		}
		int status = GetStatusCode(error);
		return Results.Problem(
			statusCode: status,
			detail: error.Message,
			instance: httpContext?.Request.Path,
			extensions: new Dictionary<string, object?>
			{
				["code"] = error.Code,
				["error"] = error.Code,
				["message"] = error.Message,
				["statusCode"] = status,
				["timestamp"] = System.DateTime.UtcNow
			});
	}

	/// <summary>Maps known application errors to HTTP statuses; other errors are client errors.</summary>
	public static int GetStatusCode(Error error) => error.Code switch
	{
		"Connection.Forbidden" or "Connection.ReadOnlyMode" => StatusCodes.Status403Forbidden,
		"User.InvalidCredentials" or "User.Unauthorized" => StatusCodes.Status401Unauthorized,
		"Storage.ConcurrencyConflict" or "Email.AlreadyExists" or "User.EmailAlreadyExists" => StatusCodes.Status409Conflict,
		var code when code.EndsWith("NotFound", System.StringComparison.Ordinal) => StatusCodes.Status404NotFound,
		_ => StatusCodes.Status400BadRequest
	};

	/// <summary>Creates a validation problem with field-level errors.</summary>
	public static IResult ValidationError(Dictionary<string, string[]> errors, HttpContext? httpContext = null) =>
		Results.ValidationProblem(errors, instance: httpContext?.Request.Path,
			extensions: new Dictionary<string, object?>
			{
				["code"] = "Validation.Failed",
				["message"] = "One or more validation errors occurred.",
				["statusCode"] = StatusCodes.Status400BadRequest,
				["timestamp"] = System.DateTime.UtcNow
			});
}

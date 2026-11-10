using Microsoft.AspNetCore.Http;
using RedisGUI.Domain.Primitives;
using RedisGUI.Server.Abstraction;
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
	public static IResult ToProblemDetails(this Error error, HttpContext httpContext)
	{
		var errorResponse = ApiErrorResponse.BadRequest(
			error.Code,
			error.Message,
			httpContext.Request.Path
		);

		return Results.Json(errorResponse, statusCode: errorResponse.StatusCode);
	}

	/// <summary>
	/// Converts an Error to a BadRequest result with structured response
	/// </summary>
	public static IResult ToBadRequest(this Error error, HttpContext? httpContext = null)
	{
		var errorResponse = ApiErrorResponse.BadRequest(
			error.Code,
			error.Message,
			httpContext?.Request.Path
		);

		return Results.Json(errorResponse, statusCode: StatusCodes.Status400BadRequest);
	}

	/// <summary>
	/// Converts an Error to a NotFound result with structured response
	/// </summary>
	public static IResult ToNotFound(this Error error, HttpContext? httpContext = null)
	{
		var errorResponse = ApiErrorResponse.NotFound(
			error.Code,
			error.Message,
			httpContext?.Request.Path
		);

		return Results.Json(errorResponse, statusCode: StatusCodes.Status404NotFound);
	}

	/// <summary>
	/// Creates a validation error result with field-level errors
	/// </summary>
	public static IResult ValidationError(Dictionary<string, string[]> errors, HttpContext? httpContext = null)
	{
		var errorResponse = ApiErrorResponse.BadRequest(
			"Validation.Failed",
			"One or more validation errors occurred",
			httpContext?.Request.Path,
			new Dictionary<string, object> { ["errors"] = errors }
		);

		return Results.Json(errorResponse, statusCode: StatusCodes.Status400BadRequest);
	}
}

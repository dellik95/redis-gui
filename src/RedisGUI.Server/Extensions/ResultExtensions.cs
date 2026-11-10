using Microsoft.AspNetCore.Http;
using RedisGUI.Shared.Primitives;
using System;

namespace RedisGUI.Server.Extensions;

/// <summary>
/// Extension methods for Result pattern to HTTP responses
/// </summary>
public static class ResultExtensions
{
	/// <summary>
	/// Converts a Result to an IResult with structured error responses
	/// </summary>
	public static IResult ToHttpResult<T>(this Result<T> result, HttpContext? httpContext = null)
	{
		if (result.IsSuccess)
		{
			return Results.Ok(result.Value);
		}

		return result.Error.ToBadRequest(httpContext);
	}

	/// <summary>
	/// Converts a Result to an IResult with structured error responses
	/// </summary>
	public static IResult ToHttpResult(this Result result, HttpContext? httpContext = null)
	{
		if (result.IsSuccess)
		{
			return Results.Ok();
		}

		return result.Error.ToBadRequest(httpContext);
	}

	/// <summary>
	/// Matches a Result to either a success or error HTTP response
	/// </summary>
	public static IResult MatchHttp<T>(
		this Result<T> result,
		Func<T, IResult> onSuccess,
		HttpContext? httpContext = null)
	{
		return result.IsSuccess
			? onSuccess(result.Value)
			: result.Error.ToBadRequest(httpContext);
	}

	/// <summary>
	/// Matches a Result to either a success or error HTTP response
	/// </summary>
	public static IResult MatchHttp(
		this Result result,
		Func<IResult> onSuccess,
		HttpContext? httpContext = null)
	{
		return result.IsSuccess
			? onSuccess()
			: result.Error.ToBadRequest(httpContext);
	}
}

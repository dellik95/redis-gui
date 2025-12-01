using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace RedisGUI.Server.Abstraction;

/// <summary>
/// Standardized API error response
/// </summary>
public sealed record ApiErrorResponse
{
	/// <summary>
	/// Error code for programmatic handling
	/// </summary>
	public required string Code { get; init; }

	/// <summary>
	/// Human-readable error message
	/// </summary>
	public required string Message { get; init; }

	/// <summary>
	/// HTTP status code
	/// </summary>
	public required int StatusCode { get; init; }

	/// <summary>
	/// Timestamp when the error occurred
	/// </summary>
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Request path where the error occurred
	/// </summary>
	public string? Path { get; init; }

	/// <summary>
	/// Additional error details (for development/debugging)
	/// </summary>
	public Dictionary<string, object>? Details { get; init; }

	/// <summary>
	/// Creates a BadRequest error response (400)
	/// </summary>
	public static ApiErrorResponse BadRequest(string code, string message, string? path = null, Dictionary<string, object>? details = null) =>
		new()
		{
			Code = code,
			Message = message,
			StatusCode = StatusCodes.Status400BadRequest,
			Path = path,
			Details = details
		};

	/// <summary>
	/// Creates a NotFound error response (404)
	/// </summary>
	public static ApiErrorResponse NotFound(string code, string message, string? path = null) =>
		new()
		{
			Code = code,
			Message = message,
			StatusCode = StatusCodes.Status404NotFound,
			Path = path
		};

	/// <summary>
	/// Creates an InternalServerError response (500)
	/// </summary>
	public static ApiErrorResponse InternalServerError(string code, string message, string? path = null, Dictionary<string, object>? details = null) =>
		new()
		{
			Code = code,
			Message = message,
			StatusCode = StatusCodes.Status500InternalServerError,
			Path = path,
			Details = details
		};

	/// <summary>
	/// Creates a Conflict error response (409)
	/// </summary>
	public static ApiErrorResponse Conflict(string code, string message, string? path = null) =>
		new()
		{
			Code = code,
			Message = message,
			StatusCode = StatusCodes.Status409Conflict,
			Path = path
		};

	/// <summary>
	/// Creates an Unauthorized error response (401)
	/// </summary>
	public static ApiErrorResponse Unauthorized(string code, string message, string? path = null) =>
		new()
		{
			Code = code,
			Message = message,
			StatusCode = StatusCodes.Status401Unauthorized,
			Path = path
		};

	/// <summary>
	/// Creates a Forbidden error response (403)
	/// </summary>
	public static ApiErrorResponse Forbidden(string code, string message, string? path = null) =>
		new()
		{
			Code = code,
			Message = message,
			StatusCode = StatusCodes.Status403Forbidden,
			Path = path
		};
}

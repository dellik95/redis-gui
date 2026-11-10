using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RedisGUI.Server.Abstraction;

/// <summary>
/// Standard API response envelope for consistent response structure
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
	/// <summary>
	/// Indicates if the request was successful
	/// </summary>
	[JsonPropertyName("success")]
	public bool Success { get; init; }

	/// <summary>
	/// The response data (null if request failed)
	/// </summary>
	[JsonPropertyName("data")]
	public T? Data { get; init; }

	/// <summary>
	/// Error information (null if request succeeded)
	/// </summary>
	[JsonPropertyName("error")]
	public ApiError? Error { get; init; }

	/// <summary>
	/// Metadata about the response (pagination, etc.)
	/// </summary>
	[JsonPropertyName("meta")]
	public Dictionary<string, object>? Meta { get; init; }

	/// <summary>
	/// Correlation ID for request tracing
	/// </summary>
	[JsonPropertyName("correlationId")]
	public string? CorrelationId { get; init; }

	/// <summary>
	/// Timestamp when the response was generated
	/// </summary>
	[JsonPropertyName("timestamp")]
	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// Creates a successful response
	/// </summary>
	public static ApiResponse<T> Ok(T data, Dictionary<string, object>? meta = null, string? correlationId = null)
	{
		return new ApiResponse<T>
		{
			Success = true,
			Data = data,
			Meta = meta,
			CorrelationId = correlationId
		};
	}

	/// <summary>
	/// Creates a failed response
	/// </summary>
	public static ApiResponse<T> Fail(string code, string message, Dictionary<string, object>? details = null, string? correlationId = null)
	{
		return new ApiResponse<T>
		{
			Success = false,
			Error = new ApiError
			{
				Code = code,
				Message = message,
				Details = details
			},
			CorrelationId = correlationId
		};
	}

	/// <summary>
	/// Creates a failed response from an error
	/// </summary>
	public static ApiResponse<T> Fail(ApiError error, string? correlationId = null)
	{
		return new ApiResponse<T>
		{
			Success = false,
			Error = error,
			CorrelationId = correlationId
		};
	}
}

/// <summary>
/// Error information in API responses
/// </summary>
public class ApiError
{
	/// <summary>
	/// Error code for programmatic handling
	/// </summary>
	[JsonPropertyName("code")]
	public string Code { get; init; } = string.Empty;

	/// <summary>
	/// Human-readable error message
	/// </summary>
	[JsonPropertyName("message")]
	public string Message { get; init; } = string.Empty;

	/// <summary>
	/// Additional error details (validation errors, etc.)
	/// </summary>
	[JsonPropertyName("details")]
	public Dictionary<string, object>? Details { get; init; }
}

/// <summary>
/// Extension methods for working with API responses
/// </summary>
public static class ApiResponseExtensions
{
	/// <summary>
	/// Converts a Result to an ApiResponse
	/// </summary>
	public static ApiResponse<T> ToApiResponse<T>(this RedisGUI.Shared.Primitives.Result<T> result, string? correlationId = null)
	{
		if (result.IsSuccess)
		{
			return ApiResponse<T>.Ok(result.Value, correlationId: correlationId);
		}

		return ApiResponse<T>.Fail(
			result.Error.Code,
			result.Error.Message,
			correlationId: correlationId);
	}

	/// <summary>
	/// Converts a non-generic Result to an ApiResponse
	/// </summary>
	public static ApiResponse<object> ToApiResponse(this RedisGUI.Shared.Primitives.Result result, string? correlationId = null)
	{
		if (result.IsSuccess)
		{
			return ApiResponse<object>.Ok(new { }, correlationId: correlationId);
		}

		return ApiResponse<object>.Fail(
			result.Error.Code,
			result.Error.Message,
			correlationId: correlationId);
	}
}

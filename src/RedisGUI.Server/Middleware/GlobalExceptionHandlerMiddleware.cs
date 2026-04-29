using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisGUI.Application.Exceptions;
using RedisGUI.Domain.Exceptions;
using RedisGUI.Domain.Primitives;
using RedisGUI.Server.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedisGUI.Server.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions and returns structured error responses
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
	private readonly RequestDelegate next;
	private readonly ILogger<GlobalExceptionHandlerMiddleware> logger;
	private readonly IWebHostEnvironment environment;
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};
	private static readonly JsonSerializerOptions JsonOptionsDev = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true
	};

	public GlobalExceptionHandlerMiddleware(
		RequestDelegate next,
		ILogger<GlobalExceptionHandlerMiddleware> logger,
		IWebHostEnvironment environment)
	{
		this.next = next;
		this.logger = logger;
		this.environment = environment;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await next(context);
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(context, ex);
		}
	}

	private async Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		string correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

		logger.LogError(
			exception,
			"Unhandled exception occurred - CorrelationId: {CorrelationId}, Path: {Path}",
			correlationId,
			context.Request.Path);

		ApiErrorResponse response = exception switch
		{
			ValidationException validationEx => HandleValidationException(context, validationEx),
			ErrorException errorEx => HandleErrorException(context, errorEx),
			ArgumentException argEx => HandleArgumentException(context, argEx),
			InvalidOperationException opEx => HandleInvalidOperationException(context, opEx),
			_ => HandleGenericException(context, exception)
		};

		context.Response.ContentType = "application/json";
		context.Response.StatusCode = response.StatusCode;

		var jsonOptions = environment.IsDevelopment() ? JsonOptionsDev : JsonOptions;

		await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
	}

	private static ApiErrorResponse HandleValidationException(HttpContext context, ValidationException exception)
	{
		Dictionary<string, string[]> errors = exception.Errors
			.GroupBy(e => e.PropertyName)
			.ToDictionary(
				g => g.Key,
				g => g.Select(e => e.ErrorMessage).ToArray());

		return ApiErrorResponse.BadRequest(
			"Validation.Failed",
			"One or more validation errors occurred",
			context.Request.Path,
			new Dictionary<string, object> { ["errors"] = errors });
	}

	private ApiErrorResponse HandleErrorException(HttpContext context, ErrorException exception)
	{
		// ErrorException wraps domain errors
		Error? firstError = exception.Errors.FirstOrDefault();
		if (firstError is null)
		{
			return ApiErrorResponse.InternalServerError(
				"Error.Unknown",
				"An unknown error occurred",
				context.Request.Path);
		}

		// Map error codes to HTTP status codes
		int statusCode = firstError.Code switch
		{
			var code when code.Contains("NotFound") => StatusCodes.Status404NotFound,
			var code when code.Contains("Unauthorized") || code.Contains("InvalidCredentials") => StatusCodes.Status401Unauthorized,
			var code when code.Contains("Forbidden") => StatusCodes.Status403Forbidden,
			var code when code.Contains("Conflict") || code.Contains("ConcurrencyConflict") => StatusCodes.Status409Conflict,
			_ => StatusCodes.Status400BadRequest
		};

		return new ApiErrorResponse
		{
			Code = firstError.Code,
			Message = firstError.Message,
			StatusCode = statusCode,
			Path = context.Request.Path,
			Details = environment.IsDevelopment()
				? new Dictionary<string, object> { ["Exception"] = exception.ToString() }
				: null
		};
	}

	private ApiErrorResponse HandleArgumentException(HttpContext context, ArgumentException exception)
	{
		return ApiErrorResponse.BadRequest(
			"Argument.Invalid",
			exception.Message,
			context.Request.Path,
			environment.IsDevelopment()
				? new Dictionary<string, object> { ["ParameterName"] = exception.ParamName ?? "Unknown" }
				: null);
	}

	private static ApiErrorResponse HandleInvalidOperationException(HttpContext context, InvalidOperationException exception)
	{
		return ApiErrorResponse.BadRequest(
			"Operation.Invalid",
			exception.Message,
			context.Request.Path);
	}

	private ApiErrorResponse HandleGenericException(HttpContext context, Exception exception)
	{
		logger.LogCritical(
			exception,
			"Unhandled exception of type {ExceptionType} - CorrelationId: {CorrelationId}",
			exception.GetType().Name,
			context.Items["CorrelationId"]);

		return ApiErrorResponse.InternalServerError(
			"Error.InternalServerError",
			environment.IsDevelopment()
				? exception.Message
				: "An internal server error occurred. Please try again later.",
			context.Request.Path,
			environment.IsDevelopment()
				? new Dictionary<string, object>
				{
					["ExceptionType"] = exception.GetType().Name,
					["StackTrace"] = exception.StackTrace ?? "No stack trace available"
				}
				: null);
	}
}


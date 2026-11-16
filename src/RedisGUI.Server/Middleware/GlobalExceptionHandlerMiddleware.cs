using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisGUI.Application.Exceptions;
using RedisGUI.Domain.Exceptions;
using RedisGUI.Domain.Primitives;
using RedisGUI.Server.Abstraction;
using RedisGUI.Server.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
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
			BadHttpRequestException badRequest => ApiErrorResponse.BadRequest(
				"Request.Invalid", badRequest.Message, context.Request.Path),
			ValidationException validationEx => HandleValidationException(context, validationEx),
			ErrorException errorEx => HandleErrorException(context, errorEx),
			ArgumentException argEx => HandleArgumentException(context, argEx),
			InvalidOperationException opEx => HandleInvalidOperationException(context, opEx),
			_ => HandleGenericException(context, exception)
		};

		var extensions = new Dictionary<string, object?>
		{
			["code"] = response.Code,
			["error"] = response.Code,
			["message"] = response.Message,
			["statusCode"] = response.StatusCode,
			["timestamp"] = response.Timestamp,
			["correlationId"] = correlationId
		};
		if (response.Details is not null) extensions["details"] = response.Details;
		await Results.Problem(statusCode: response.StatusCode, detail: response.Message,
			instance: context.Request.Path, extensions: extensions).ExecuteAsync(context);
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
		int statusCode = ErrorResultExtensions.GetStatusCode(firstError);

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


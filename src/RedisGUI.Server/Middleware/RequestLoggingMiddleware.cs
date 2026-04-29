using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisGUI.Server.Middleware;

/// <summary>
/// Middleware that logs HTTP requests and responses for observability
/// </summary>
public sealed class RequestLoggingMiddleware
{
	private readonly RequestDelegate next;
	private readonly ILogger<RequestLoggingMiddleware> logger;

	public RequestLoggingMiddleware(
		RequestDelegate next,
		ILogger<RequestLoggingMiddleware> logger)
	{
		this.next = next;
		this.logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		string correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

		// Log request
		logger.LogInformation(
			"HTTP {Method} {Path} started - CorrelationId: {CorrelationId}",
			context.Request.Method,
			context.Request.Path,
			correlationId);

		try
		{
			await next(context);
			stopwatch.Stop();

			// Log successful response
			logger.LogInformation(
				"HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms - CorrelationId: {CorrelationId}",
				context.Request.Method,
				context.Request.Path,
				context.Response.StatusCode,
				stopwatch.ElapsedMilliseconds,
				correlationId);
		}
		catch (Exception ex)
		{
			stopwatch.Stop();

			// Log error
			logger.LogError(
				ex,
				"HTTP {Method} {Path} failed with exception after {ElapsedMs}ms - CorrelationId: {CorrelationId}",
				context.Request.Method,
				context.Request.Path,
				stopwatch.ElapsedMilliseconds,
				correlationId);

			throw;
		}
	}
}


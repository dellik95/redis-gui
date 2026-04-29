using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisGUI.Server.Middleware;

/// <summary>
/// Adds a correlation ID to each request for traceability.
/// </summary>
public sealed class CorrelationIdMiddleware
{
	private const string headerName = "X-Correlation-Id";
	private readonly RequestDelegate next;

	public CorrelationIdMiddleware(RequestDelegate next)
	{
		this.next = next;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// Prefer client-provided ID if present, otherwise generate one.
		string correlationId = context.Request.Headers.TryGetValue(headerName, out StringValues values)
			? values.ToString()
			: ActivityTraceId.CreateRandom().ToString();

		context.TraceIdentifier = correlationId;
		context.Response.Headers[headerName] = correlationId;

		using IDisposable? scope = context.RequestServices
				   .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
				   .BeginScope(new Dictionary<string, object>
				   {
					   ["CorrelationId"] = correlationId
				   });
		await next(context);
	}
}



using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

namespace RedisGUI.Server.Observability;

/// <summary>
/// Configures OpenTelemetry tracing, metrics, and logging for the API.
/// </summary>
public static class OpenTelemetryConfigurationExtensions
{
	public static IServiceCollection AddApiOpenTelemetry(this IServiceCollection services)
	{
		var serviceName = "RedisGUI.Server";
		var serviceVersion = "1.0.0";

		services.AddOpenTelemetry()
			.ConfigureResource(resource => resource
				.AddService(serviceName, serviceVersion: serviceVersion)
				.AddTelemetrySdk()
				.AddEnvironmentVariableDetector())
			.WithTracing(tracing =>
			{
				tracing
					.AddAspNetCoreInstrumentation(options =>
					{
						// Enrich spans with additional request information
						options.RecordException = true;
						options.Filter = httpContext =>
						{
							// Don't trace health check endpoints
							return !httpContext.Request.Path.StartsWithSegments("/health");
						};
					})
					.AddHttpClientInstrumentation(options =>
					{
						options.RecordException = true;
					})
					.AddEntityFrameworkCoreInstrumentation(options =>
					{
					// Note: SetDbStatementForText and SetDbStatementForStoredProcedure are not available in current version
					// options.SetDbStatementForText = true;
					// options.SetDbStatementForStoredProcedure = true;
					options.EnrichWithIDbCommand = (activity, command) =>
					{
						// Add custom tags to EF Core spans
						activity.SetTag("db.command.timeout", command.CommandTimeout);
					};
				})
				// Add source for custom instrumentation
				.AddSource("RedisGUI.*")
				.AddConsoleExporter();
				
			// TODO: Add production exporters when deploying
			// .AddJaegerExporter(options => options.Endpoint = new Uri("http://jaeger:4317"))
			// .AddOtlpExporter(options => options.Endpoint = new Uri("http://otel-collector:4317"))
		})
			.WithMetrics(metrics =>
			{
				metrics
					.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddRuntimeInstrumentation()
					.AddProcessInstrumentation()
					// Add custom meters
					.AddMeter("RedisGUI.*")
					.AddConsoleExporter();
					
				// TODO: Add production exporters
				// .AddPrometheusExporter()
				// .AddOtlpExporter()
			});

		return services;
	}

	/// <summary>
	/// Configures OpenTelemetry logging (optional alternative to Serilog)
	/// </summary>
	public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder logging)
	{
		logging.AddOpenTelemetry(options =>
		{
			options.IncludeScopes = true;
			options.ParseStateValues = true;
			options.IncludeFormattedMessage = true;
			
			// Add resource attributes
			options.SetResourceBuilder(
				ResourceBuilder.CreateDefault()
					.AddService("RedisGUI.Server"));
					
			// TODO: Add production exporters
			// options.AddOtlpExporter();
		});

		return logging;
	}
}



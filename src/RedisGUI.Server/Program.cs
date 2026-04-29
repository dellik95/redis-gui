using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedisGUI.Application;
using RedisGUI.Infrastructure;
using RedisGUI.Infrastructure.SignalR.Hubs;
using RedisGUI.Server.Extensions;
using RedisGUI.Server.Logging;
using RedisGUI.Server.Middleware;
using RedisGUI.Server.Observability;
using RedisGUI.Server.OpenApi;
using System;

namespace RedisGUI.Server;

/// <summary>
/// Main entry point class for the Redis GUI application
/// </summary>
public class Program
{
	/// <summary>
	/// Application entry point that configures and runs the web application
	/// </summary>
	/// <param name="args">Command line arguments</param>
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		builder.Logging.ConfigureApplicationLogging();
		builder.Services.AddApiOpenTelemetry();

		// Configure response compression
		builder.Services.AddResponseCompression(options =>
		{
			options.EnableForHttps = true;
			options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
			options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
		});

		// Configure CORS
		builder.Services.AddCors(options =>
		{
			options.AddPolicy("DefaultPolicy", policy =>
			{
				policy.WithOrigins(
						"http://localhost:4200",
						"https://localhost:4200")
					.AllowAnyMethod()
					.AllowAnyHeader()
					.AllowCredentials(); // Required for SignalR
			});
		});

		builder.Services.AddSwaggerGen();
		builder.Services.AddApplication();
		builder.Services.AddInfrastructure(builder.Configuration);
		builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
		builder.Services.ConfigureOptions<ConfigureSwaggerUiOptions>();
		builder.Services.AddEndpoints(typeof(Program).Assembly);
		builder.Services.AddOutputCache(options =>
		{
			// Default cache policy
			options.AddBasePolicy(builder => builder
				.Expire(TimeSpan.FromSeconds(60))
				.Tag("default"));
		});
		builder.Services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
			options.AddFixedWindowLimiter("auth", limiter =>
			{
				limiter.Window = TimeSpan.FromMinutes(1);
				limiter.PermitLimit = 5;
				limiter.QueueLimit = 0;
			})
			// General fallback limiter for sensitive endpoints (import/export/transactions/lua)
			.AddFixedWindowLimiter("sensitive", limiter =>
			{
				limiter.Window = TimeSpan.FromMinutes(1);
				limiter.PermitLimit = 30;
				limiter.QueueLimit = 0;
			});
		});

		WebApplication app = builder.Build();
		
		// Enable response compression
		app.UseResponseCompression();
		
		// Enable CORS
		app.UseCors("DefaultPolicy");
		
		app.UseDefaultFiles();
		app.UseStaticFiles();

		app.UseMiddleware<CorrelationIdMiddleware>();
		app.UseMiddleware<RequestLoggingMiddleware>();
		app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
		app.UseOutputCache();
		app.UseRateLimiter();

		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
			app.ApplyMigrations();
			app.SeedFakeData();
		}

		app.UseAuthentication();
		app.UseAuthorization();

		// SignalR hubs
		app.MapHub<RedisMetricsHub>("/hub/metrics");
		app.MapHub<PubSubHub>("/hub/pubsub");
		app.MapHub<KeyChangeHub>("/hub/keychange");

		app.MapHealthChecks("/health");
		app.MapHealthChecks("/health/ready");


		ApiVersionSet apiVersionSet = app.NewApiVersionSet()
			.HasApiVersion(new ApiVersion(1.0))
			.ReportApiVersions()
			.Build();

		RouteGroupBuilder group = app.MapGroup("api/v{version:apiVersion}")
			.WithApiVersionSet(apiVersionSet);

		app.MapEndpoints(group);

		app.Run();
	}
}

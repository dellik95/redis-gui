using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedisGUI.Application;
using RedisGUI.Infrastructure;
using RedisGUI.Infrastructure.SignalR.Hubs;
using RedisGUI.Server.Endpoints.Connections;
using RedisGUI.Server.Endpoints.Redis;
using RedisGUI.Server.Extensions;
using RedisGUI.Server.OpenApi;

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
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddSwaggerGen();
		builder.Services.AddApplication();
		builder.Services.AddInfrastructure(builder.Configuration);
		builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
		builder.Services.ConfigureOptions<ConfigureSwaggerUiOptions>();

		var app = builder.Build();
		app.UseDefaultFiles();
		app.UseStaticFiles();

		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
			app.ApplyMigrations();
			//app.SeedFakeData();
		}

		app.MapHub<RedisMetricsHub>("/hub/metrics");
		app.MapConnectionsEndpoints();
		app.MapRedisEndpoints();


		app.Run();
	}
}

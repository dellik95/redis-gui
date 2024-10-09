using RedisGUI.Application;
using RedisGUI.Infrastructure;
using RedisGUI.Server.Extensions;
using RedisGUI.Server.OpenApi;

namespace RedisGUI.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddAuthorization();

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
			}

			app.UseAuthorization();
			app.MapControllers();

			app.Run();
		}
	}
}

using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Cryptography;
using RedisGUI.Infrastructure.Persistence;
using RedisGUI.Infrastructure.Persistence.Repositories;
using RedisGUI.Infrastructure.Redis;

namespace RedisGUI.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<PasswordEncryptor>();
		services.AddSingleton<IPasswordDecryptor>(svc => svc.GetRequiredService<PasswordEncryptor>());
		services.AddSingleton<IPasswordEncryptor>(svc => svc.GetRequiredService<PasswordEncryptor>());

		services.AddSingleton<IConnectionPool, ConnectionPool>();
		services.AddScoped<IConnectionService, ConnectionService>();

		AddPersistence(services, configuration);

		AddSecurity(services, configuration);

		AddHealthChecks(services, configuration);

		AddApiVersioning(services);

		AddBackgroundJobs(services, configuration);

		return services;
	}

	private static void AddSecurity(IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<CryptographyConfiguration>()
			.Bind(configuration.GetSection(CryptographyConfiguration.Key))
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddScoped<IPasswordEncryptor, PasswordEncryptor>();
	}

	private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<DatabaseConfiguration>(configuration.GetSection(DatabaseConfiguration.Key));
		services.AddDbContext<ApplicationDbContext>((sp, cgf) =>
		{
			var options = sp.GetRequiredService<IOptions<DatabaseConfiguration>>();
			var config = options.Value;

			if (config.IsInMemory)
			{
				cgf.UseInMemoryDatabase("ApplicationDbContext");
				return;
			}

			cgf.UseSqlServer(config.ConnectionString, builder =>
			{
				builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
			});

		});
		services.AddTransient<IRedisConnectionRepository, RedisConnectionRepository>();
		services.AddScoped<IUnitOfWork>(p => p.GetRequiredService<ApplicationDbContext>());
	}

	private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
	{
		//TODO: add health Checks
		// 1. Database
		// 2. Cache
	}

	private static void AddApiVersioning(IServiceCollection services)
	{
		services.AddApiVersioning(options =>
			{
				options.DefaultApiVersion = new ApiVersion(1);
				options.ReportApiVersions = true;
				options.AssumeDefaultVersionWhenUnspecified = true;
				options.ApiVersionReader = ApiVersionReader.Combine(
					new UrlSegmentApiVersionReader(),
					new HeaderApiVersionReader("X-Api-Version"));
			})
			.AddMvc()
			.AddApiExplorer(options =>
			{
				options.GroupNameFormat = "'v'V";
				options.SubstituteApiVersionInUrl = true;
			});
	}

	private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
	{
		//TODO: Add background job to check redis state
	}
}
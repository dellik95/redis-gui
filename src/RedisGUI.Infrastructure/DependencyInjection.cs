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

/// <summary>
/// Extension methods for configuring infrastructure services in the DI container
/// </summary>
public static class DependencyInjection
{
	/// <summary>
	/// Adds infrastructure services to the service collection
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	/// <param name="configuration">Application configuration</param>
	/// <returns>The service collection for chaining</returns>
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddSingleton<PasswordEncryptor>();
		services.AddSingleton<IPasswordDecrypt>(svc => svc.GetRequiredService<PasswordEncryptor>());
		services.AddSingleton<IPasswordEncryptor>(svc => svc.GetRequiredService<PasswordEncryptor>());


		services.AddOptions<ConnectionPoolOptions>()
			.Bind(configuration.GetSection(ConnectionPoolOptions.Key));

		services.AddSingleton<IConnectionPool, ConnectionPool>();
		services.AddScoped<IConnectionService, ConnectionService>();

		AddPersistence(services, configuration);

		AddSecurity(services, configuration);

		AddHealthChecks(services, configuration);

		AddApiVersioning(services);

		AddBackgroundJobs(services, configuration);

		return services;
	}

	/// <summary>
	/// Adds security services to the service collection
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	/// <param name="configuration">Application configuration</param>
	private static void AddSecurity(IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<CryptographyConfiguration>()
			.Bind(configuration.GetSection(CryptographyConfiguration.Key))
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddScoped<IPasswordEncryptor, PasswordEncryptor>();
	}

	/// <summary>
	/// Adds persistence services to the service collection
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	/// <param name="configuration">Application configuration</param>
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

			cgf.UseMySQL(config.ConnectionString, builder => { builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); });
		});
		services.AddTransient<IRedisConnectionRepository, RedisConnectionRepository>();
		services.AddTransient(typeof(IRepository<>), typeof(RepositoryBase<>));
		services.AddScoped<IUnitOfWork>(p => p.GetRequiredService<ApplicationDbContext>());
	}

	/// <summary>
	/// Adds health check services to the service collection
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	/// <param name="configuration">Application configuration</param>
	private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
	{
		//TODO: add health Checks
		// 1. Database
		// 2. Cache
	}

	/// <summary>
	/// Adds API versioning services to the service collection
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	private static void AddApiVersioning(IServiceCollection services)
	{
		services
			.AddEndpointsApiExplorer()
			.AddApiVersioning(options =>
			{
				options.DefaultApiVersion = new ApiVersion(1);
				options.ReportApiVersions = true;
				options.AssumeDefaultVersionWhenUnspecified = true;
				options.ApiVersionReader = ApiVersionReader.Combine(
					new UrlSegmentApiVersionReader(),
					new HeaderApiVersionReader("X-Api-Version"));
			})
			.AddApiExplorer(options =>
			{
				options.GroupNameFormat = "'v'V";
				options.SubstituteApiVersionInUrl = true;
			});
	}

	/// <summary>
	/// Adds background job services to the service collection
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	/// <param name="configuration">Application configuration</param>
	/// <remarks>
	/// Currently a placeholder for future implementation of background jobs including:
	/// - Database maintenance jobs
	/// - Cache cleanup jobs
	/// </remarks>
	private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
	{
		// TODO: Implement background job registration
	}
}

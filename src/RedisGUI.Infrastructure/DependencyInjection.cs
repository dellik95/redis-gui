using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using RedisGUI.Application.Core.Services;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.RedisMetrics.Abstractions;
using RedisGUI.Domain.Users;
using RedisGUI.Infrastructure.Authentication;
using RedisGUI.Infrastructure.BackgroundJobs;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.Cryptography;
using RedisGUI.Infrastructure.HealthChecks;
using RedisGUI.Infrastructure.Persistence;
using RedisGUI.Infrastructure.Persistence.Cache;
using RedisGUI.Infrastructure.Persistence.Interceptors;
using RedisGUI.Infrastructure.Persistence.Repositories;
using RedisGUI.Infrastructure.Persistence.SqlProcessor;
using RedisGUI.Infrastructure.Redis;
using RedisGUI.Infrastructure.Redis.Mock;
using RedisGUI.Infrastructure.RedisMetrics;
using RedisGUI.Infrastructure.Services;
using RedisGUI.Infrastructure.SignalR;
using System.Linq;
using System.Text;

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
		// Register PasswordEncryptor for both interfaces
		services.AddSingleton<PasswordEncryptor>();
		services.AddSingleton<IPasswordDecrypt, PasswordEncryptor>(sp => sp.GetRequiredService<PasswordEncryptor>());
		services.AddSingleton<IPasswordEncryptor, PasswordEncryptor>(sp => sp.GetRequiredService<PasswordEncryptor>());

		services.AddOptions<RedisMockOptions>()
			.Bind(configuration.GetSection(RedisMockOptions.Key))
			.ValidateDataAnnotations()
			.ValidateOnStart();
		RedisMockOptions mockOptions = configuration.GetSection(RedisMockOptions.Key).Get<RedisMockOptions>() ?? new RedisMockOptions();

		services.AddOptions<ConnectionPoolOptions>()
			.Bind(configuration.GetSection(ConnectionPoolOptions.Key));

		services.AddSingleton<IConnectionPool, ConnectionPool>();
		
		// Register specialized Redis services
		if (mockOptions.Enabled)
		{
			// Register shared mock store as singleton
			services.AddSingleton<MockRedisStore>(sp =>
			{
				var logger = sp.GetService<ILogger<MockRedisStore>>();
				var store = new MockRedisStore(logger);
				store.SeedData(mockOptions.DummyDataKeyCount);
				return store;
			});

			// Register mock implementations for specialized services
			services.AddSingleton<IRedisConnectionService, MockRedisConnectionService>();
			services.AddSingleton<IRedisKeyService, MockRedisKeyService>();
			services.AddSingleton<IRedisValueService, MockRedisValueService>();
			services.AddSingleton<IRedisPubSubService, MockRedisPubSubService>();
			services.AddSingleton<IRedisTransactionService, MockRedisTransactionService>();
			services.AddSingleton<IRedisScriptService, MockRedisScriptService>();
			
			// Keep legacy IConnectionService for backward compatibility
			services.AddSingleton<IConnectionService, ConnectionService>();
		}
		else
		{
			// Register real Redis implementations
			services.AddScoped<IRedisConnectionService, RedisConnectionService>();
			services.AddScoped<IRedisKeyService, RedisKeyService>();
			services.AddScoped<IRedisValueService, RedisValueService>();
			services.AddScoped<IRedisPubSubService, RedisPubSubService>();
			services.AddScoped<IRedisTransactionService, RedisTransactionService>();
			services.AddScoped<IRedisScriptService, RedisScriptService>();
			
			// Keep legacy IConnectionService for backward compatibility
			services.AddScoped<IConnectionService, ConnectionService>();
		}

		AddAuthentication(services, configuration);

		AddPersistence(services, configuration);

		AddSecurity(services, configuration);

		AddHealthChecks(services, configuration);

		AddApiVersioning(services);

		AddBackgroundJobs(services);

		AddSignalR(services);

		AddRedisMetrics(services, configuration);

		AddCaching(services, configuration);

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
	/// Adds authentication and authorization services
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	/// <param name="configuration">Application configuration</param>
	private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
	{
		// Configure JWT settings with validation
		services.AddOptions<JwtSettings>()
			.Bind(configuration.GetSection(JwtSettings.SectionName))
			.ValidateDataAnnotations()
			.ValidateOnStart();

		// Register authentication services
		services.AddScoped<IPasswordHasher, PasswordHasher>();
		services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
		services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddHttpContextAccessor();

		JwtSettings? jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
		if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
		{
			throw new InvalidOperationException("JWT settings are not configured properly");
		}

		services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		})
		.AddJwtBearer(options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = jwtSettings.Issuer,
				ValidAudience = jwtSettings.Audience,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
			};
		});

		services.AddAuthorization(options =>
		{
			// Admin-only policy
			options.AddPolicy(Authorization.Policies.AdminOnly, policy =>
				policy.RequireRole("Admin"));

			// Write access policy (Admin or User, excludes ReadOnly)
			options.AddPolicy(Authorization.Policies.WriteAccess, policy =>
				policy.RequireRole("Admin", "User"));

			// Authenticated policy
			options.AddPolicy(Authorization.Policies.Authenticated, policy =>
				policy.RequireAuthenticatedUser());
		});

		// Register authorization handlers
		services.AddScoped<IAuthorizationHandler, Authorization.ConnectionOwnershipHandler>();
	}

	/// <summary>
	/// Adds persistence services to the service collection
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	/// <param name="configuration">Application configuration</param>
	private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<DatabaseConfiguration>(configuration.GetSection(DatabaseConfiguration.Key));
		services.AddSingleton<IDbCommandInterceptorProcessor, DbCommandInterceptorProcessor>();
		services.AddSingleton<ISqlCommandsProcessor, SqlCommandsProcessor>();
		services.AddSingleton<ICacheKeyProvider, CacheKeyProvider>();
		services.AddScoped<CachingInterceptor>();
		services.AddScoped<DomainEventsDispatcherInterceptor>();

		services.AddDbContext<ApplicationDbContext>((sp, cgf) =>
			{
				IOptions<DatabaseConfiguration> options = sp.GetRequiredService<IOptions<DatabaseConfiguration>>();
				DatabaseConfiguration config = options.Value;

				if (config.IsInMemory)
				{
					cgf.UseInMemoryDatabase("ApplicationDbContext");
					return;
				}

				cgf.UseMySql(config.ConnectionString, ServerVersion.AutoDetect(config.ConnectionString), builder =>
				{
					builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
				}).AddInterceptors(
					sp.GetRequiredService<CachingInterceptor>(),
					sp.GetRequiredService<DomainEventsDispatcherInterceptor>());
			});

		services.AddTransient<IRedisConnectionRepository, RedisConnectionRepository>();
		services.AddTransient<IUserRepository, UserRepository>();
		services.AddTransient<IAuditLogRepository, AuditLogRepository>();
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
		DatabaseConfiguration? dbConfig = configuration.GetSection(DatabaseConfiguration.Key).Get<DatabaseConfiguration>();
		RedisHealthCheckOptions? redisHealthOptions = configuration.GetSection(RedisHealthCheckOptions.Key).Get<RedisHealthCheckOptions>();

		services.Configure<RedisHealthCheckOptions>(configuration.GetSection(RedisHealthCheckOptions.Key));

		IHealthChecksBuilder healthChecks = services.AddHealthChecks();

		// Database health (works for in-memory and relational)
		healthChecks.AddCheck<DatabaseHealthCheck>(
			"database",
			tags: ["db", "sql", "ready"]);

		// Redis health (optional - only if connection string configured)
		if (redisHealthOptions is { ConnectionString: { Length: > 0 } })
		{
			healthChecks.AddCheck<RedisHealthCheck>(
				"redis",
				tags: ["cache", "redis", "ready"]);
		}

		// Memory health check
		healthChecks.AddCheck("memory", () =>
		{
			long allocated = GC.GetTotalMemory(forceFullCollection: false);
			long threshold = 1024L * 1024L * 1024L; // 1GB
			long allocatedMB = allocated / 1024 / 1024;

			return allocated < threshold
				? HealthCheckResult.Healthy($"Memory usage: {allocatedMB}MB")
				: HealthCheckResult.Degraded($"Memory usage high: {allocatedMB}MB");
		}, tags: new[] { "memory" });

		// Background services health check
		healthChecks.AddCheck<BackgroundServicesHealthCheck>(
			"background-services",
			tags: ["services", "background", "ready"]);
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


	private static void AddBackgroundJobs(IServiceCollection services)
	{
		services.AddHostedService<RedisMetricsBackgroundJob>();
		services.AddHostedService<ConnectionHealthMonitor>();
		services.AddSingleton<IConnectionHealthMonitor>(sp =>
			sp.GetServices<IHostedService>()
				.OfType<ConnectionHealthMonitor>()
				.First());
	}

	private static void AddSignalR(IServiceCollection services)
	{
		// Metrics hub services
		services.AddSingleton<INotificationService, NotificationService>();
		services.AddSingleton<IHubSubscribersManager, HubSubscribersManager>();

		// Pub/Sub hub services
		services.AddSingleton<IPubSubSubscriptionManager, PubSubSubscriptionManager>();
		services.AddSingleton<IPubSubNotificationService, PubSubNotificationService>();

		// Key change hub services
		services.AddSingleton<IKeyChangeSubscriptionManager, KeyChangeSubscriptionManager>();
		services.AddSingleton<IKeyChangeNotificationService, KeyChangeNotificationService>();

		services.AddSignalR();
	}

	private static void AddRedisMetrics(IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<MetricsCollectorOptions>(configuration.GetSection(MetricsCollectorOptions.Key));
		
		// Use mock metrics collector if Redis mock is enabled
		RedisMockOptions mockOptions = configuration.GetSection(RedisMockOptions.Key).Get<RedisMockOptions>() ?? new RedisMockOptions();
		if (mockOptions.Enabled)
		{
			services.AddSingleton<IMetricsCollector, MockMetricsCollector>();
		}
		else
		{
			services.AddSingleton<IMetricsCollector, RedisMetricsCollector>();
		}
	}


	private static void AddCaching(IServiceCollection services, IConfiguration configuration)
	{
		services.AddOptions<CacheConfiguration>()
			.Bind(configuration.GetSection(CacheConfiguration.Key))
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddFusionCache();
	}
}

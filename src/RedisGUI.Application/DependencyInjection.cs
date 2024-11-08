using Microsoft.Extensions.DependencyInjection;
using RedisGUI.Application.Core.Behavior;

namespace RedisGUI.Application;

/// <summary>
/// Extension methods for configuring application services in the DI container
/// </summary>
public static class DependencyInjection
{
	/// <summary>
	/// Adds application services to the service collection
	/// </summary>
	/// <param name="services">The service collection to add services to</param>
	/// <returns>The service collection for chaining</returns>
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		services.AddMediatR(config =>
		{
			config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
			config.AddOpenBehavior(typeof(LoggingBehavior<,>));
			config.AddOpenBehavior(typeof(ValidationBehavior<,>));
		});

		return services;
	}
}

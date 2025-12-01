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
	public static IServiceCollection AddApplicationMediator(this IServiceCollection services)
	{
		// Invoke shared mediator registration with no additional behaviors.
		RedisGUI.Shared.Mediator.ServiceCollectionExtensions.AddCustomMediator(services, static config =>
		{
			config.RegisterHandlersFromAssembly(typeof(DependencyInjection).Assembly);
		});
		return services;
	}

	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		services.AddApplicationMediator();
		return services;
	}
}

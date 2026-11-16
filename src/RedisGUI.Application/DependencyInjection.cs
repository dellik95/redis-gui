using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Linq;

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
		// Invoke shared mediator registration with validation behavior
		RedisGUI.Shared.Mediator.ServiceCollectionExtensions.AddCustomMediator(services, static config =>
		{
			config.RegisterHandlersFromAssembly(typeof(DependencyInjection).Assembly);
		});
		return services;
	}

	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		// Register concrete validators, including internal use-case validators.
		foreach (var type in typeof(DependencyInjection).Assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }))
		{
			foreach (var service in type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
				services.AddScoped(service, type);
		}

		// Register mediator with behaviors
		services.AddApplicationMediator();

		return services;
	}
}

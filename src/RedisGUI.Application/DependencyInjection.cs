using FluentValidation;
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
		// Invoke shared mediator registration with validation behavior
		RedisGUI.Shared.Mediator.ServiceCollectionExtensions.AddCustomMediator(services, static config =>
		{
			config.RegisterHandlersFromAssembly(typeof(DependencyInjection).Assembly);
		});
		return services;
	}

	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		// Register FluentValidation validators from assembly
		// Note: AddValidatorsFromAssembly requires FluentValidation.DependencyInjectionExtensions package
		// services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

		// Register mediator with behaviors
		services.AddApplicationMediator();

		return services;
	}
}

using DispatchWeave;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Application.Core.Services;
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
		services.AddDispatchWeave(builder => builder
			.RegisterHandlers(typeof(DependencyInjection).Assembly)
			.WithValidation()
			.WithLogging()
			.AddBehavior(typeof(AuditingBehavior<,>)));
		return services;
	}

	/// <summary>Registers a background job handler without modifying central registries or switch statements.</summary>
	public static IServiceCollection AddBackgroundJobHandler<THandler>(this IServiceCollection services)
		where THandler : class, IBackgroundJobHandler
	{
		services.TryAddEnumerable(ServiceDescriptor.Scoped<IBackgroundJobHandler, THandler>());
		return services;
	}

	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		var assembly = typeof(DependencyInjection).Assembly;
		var concreteTypes = assembly.GetTypes()
			.Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
			.ToArray();

		// Register all IBackgroundJobHandler implementations discovered in the Application assembly.
		foreach (var type in concreteTypes.Where(t => typeof(IBackgroundJobHandler).IsAssignableFrom(t)))
		{
			services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IBackgroundJobHandler), type));
		}

		// Register concrete validators, including internal use-case validators.
		foreach (var type in concreteTypes)
		{
			foreach (var service in type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
			{
				services.AddScoped(service, type);
			}
		}

		// Register mediator with behaviors
		services.AddApplicationMediator();

		return services;
	}
}

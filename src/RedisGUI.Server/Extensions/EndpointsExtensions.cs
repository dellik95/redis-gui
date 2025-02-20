using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Extensions
{
	/// <summary>
	/// Provides extension methods for configuring and mapping endpoints in the application.
	/// </summary>
	public static class EndpointsExtensions
	{
		/// <summary>
		/// Registers all endpoint implementations from the specified assembly into the service collection.
		/// </summary>
		/// <param name="serviceCollection">The service collection to add the endpoints to.</param>
		/// <param name="assembly">The assembly to scan for endpoint implementations.</param>
		/// <returns>The service collection for chaining.</returns>
		/// <remarks>
		/// This method scans the provided assembly for non-abstract classes implementing IEndpoint
		/// and registers them as transient services.
		/// </remarks>
		public static IServiceCollection AddEndpoints(
			this IServiceCollection serviceCollection,
			Assembly assembly)
		{
			var serviceDescriptors = assembly
				.DefinedTypes
				.Where(type => type is { IsAbstract: false, IsInterface: false } &&
							   type.IsAssignableTo(typeof(IEndpoint)))
				.Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
				.ToArray();

			serviceCollection.TryAddEnumerable(serviceDescriptors);
			return serviceCollection;
		}

		/// <summary>
		/// Maps all registered endpoints to the application's routing configuration.
		/// </summary>
		/// <param name="app">The web application instance.</param>
		/// <param name="routeGroupBuilder">Optional route group builder for grouping endpoints.</param>
		/// <returns>The web application instance for chaining.</returns>
		/// <remarks>
		/// This method resolves all registered IEndpoint implementations and calls their MapEndpoint method
		/// to configure their routes in the application.
		/// </remarks>
		public static WebApplication MapEndpoints(this WebApplication app, RouteGroupBuilder routeGroupBuilder = null)
		{
			var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

			IEndpointRouteBuilder builder =
				routeGroupBuilder is null ? app : routeGroupBuilder;

			foreach (var endpoint in endpoints)
			{
				endpoint.MapEndpoint(builder);
			}

			return app;
		}
	}
}

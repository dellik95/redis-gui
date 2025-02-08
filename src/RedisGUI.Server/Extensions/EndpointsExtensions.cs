using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RedisGUI.Server.Abstraction;

namespace RedisGUI.Server.Extensions
{
	public static class EndpointsExtensions
	{
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


		public static WebApplication MapEndpoints(this WebApplication app, RouteGroupBuilder? routeGroupBuilder = null)
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

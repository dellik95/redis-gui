using Microsoft.Extensions.DependencyInjection;
using RedisGUI.Application.Core.Behavior;

namespace RedisGUI.Application;

public static class DependencyInjection
{
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
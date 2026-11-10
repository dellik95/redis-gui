using Microsoft.Extensions.DependencyInjection;

namespace RedisGUI.Shared.Mediator
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomMediator(this IServiceCollection services, Action<MediatorBuilder> configure)
        {
            var builder = new MediatorBuilder();
            configure(builder);
            services.AddScoped<IMediator>(sp => builder.Build(type => ActivatorUtilities.CreateInstance(sp, type)));
            return services;
        }
    }
}

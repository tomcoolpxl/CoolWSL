using Microsoft.Extensions.DependencyInjection;

namespace CoolWSL.Configuration.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ConfigurationModuleMarker>();

        return services;
    }
}
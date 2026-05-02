using Microsoft.Extensions.DependencyInjection;
using CoolWSL.Configuration.Services;
using CoolWSL.Core.Abstractions;

namespace CoolWSL.Configuration.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ConfigurationModuleMarker>();
        services.AddSingleton<IWslGlobalConfigService, WslGlobalConfigService>();

        return services;
    }
}

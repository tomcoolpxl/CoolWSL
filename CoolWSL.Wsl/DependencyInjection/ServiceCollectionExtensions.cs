using Microsoft.Extensions.DependencyInjection;

namespace CoolWSL.Wsl.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslWsl(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<WslModuleMarker>();

        return services;
    }
}
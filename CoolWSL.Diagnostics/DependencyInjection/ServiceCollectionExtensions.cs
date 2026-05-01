using Microsoft.Extensions.DependencyInjection;

namespace CoolWSL.Diagnostics.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslDiagnostics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<DiagnosticsModuleMarker>();

        return services;
    }
}
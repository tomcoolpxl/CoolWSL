using Microsoft.Extensions.DependencyInjection;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.Diagnostics.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslDiagnostics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDashboardStatusService, DashboardStatusService>();
        services.AddSingleton<DiagnosticsModuleMarker>();

        return services;
    }
}
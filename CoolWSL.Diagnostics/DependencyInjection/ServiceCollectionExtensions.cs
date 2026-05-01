using Microsoft.Extensions.DependencyInjection;
using CoolWSL.Diagnostics.Mappers;
using CoolWSL.Diagnostics.Services;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.Diagnostics.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslDiagnostics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDashboardStatusService, DashboardStatusService>();
        services.AddSingleton<DiagnosticSummaryMapper>();
        services.AddSingleton<IDiagnosticsService, DiagnosticsService>();
        services.AddSingleton<DiagnosticsModuleMarker>();

        return services;
    }
}
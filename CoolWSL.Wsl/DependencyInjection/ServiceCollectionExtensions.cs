using Microsoft.Extensions.DependencyInjection;
using CoolWSL.Core.Abstractions;
using CoolWSL.Wsl.Errors;
using CoolWSL.Wsl.Parsing;
using CoolWSL.Wsl.Services;

namespace CoolWSL.Wsl.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslWsl(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<WslErrorMapper>();
        services.AddSingleton<WslListParser>();
        services.AddSingleton<WslStatusParser>();
        services.AddSingleton<IWslCommandService, WslCommandService>();
        services.AddSingleton<IWslDistroService, WslDistroService>();
        services.AddSingleton<WslModuleMarker>();

        return services;
    }
}
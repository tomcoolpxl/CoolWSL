using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CoolWSL.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<FileAppLogger>();
        services.AddSingleton<IAppLogger>(static serviceProvider => serviceProvider.GetRequiredService<FileAppLogger>());
        services.AddSingleton<IAppLogReader>(static serviceProvider => serviceProvider.GetRequiredService<FileAppLogger>());

        return services;
    }
}

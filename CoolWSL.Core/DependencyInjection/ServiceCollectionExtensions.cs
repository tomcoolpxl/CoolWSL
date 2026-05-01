using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CoolWSL.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoolWslCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAppLogger, NullAppLogger>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
using CoolWSL.App.ViewModels;
using CoolWSL.App.Views;
using CoolWSL.Configuration.DependencyInjection;
using CoolWSL.Core.DependencyInjection;
using CoolWSL.Diagnostics.DependencyInjection;
using CoolWSL.Wsl.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CoolWSL.App.DependencyInjection;

public static class AppServiceCollection
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services
            .AddCoolWslCore()
            .AddCoolWslWsl()
            .AddCoolWslConfiguration()
            .AddCoolWslDiagnostics();

        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<ShellPage>();
        services.AddTransient<Views.PlaceholderPage>();

        return services.BuildServiceProvider();
    }
}
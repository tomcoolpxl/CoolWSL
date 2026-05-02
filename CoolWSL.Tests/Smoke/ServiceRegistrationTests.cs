using CoolWSL.App.DependencyInjection;
using CoolWSL.App.ViewModels;
using CoolWSL.Configuration.DependencyInjection;
using CoolWSL.Core.Abstractions;
using CoolWSL.Diagnostics.DependencyInjection;
using CoolWSL.Diagnostics.Services;
using CoolWSL.Diagnostics.Status;
using CoolWSL.Wsl.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Smoke;

[TestClass]
public sealed class ServiceRegistrationTests
{
    [TestMethod]
    public void RegistersShellAndModuleBoundaries()
    {
        using var services = AppServiceCollection.Build() as ServiceProvider;

        Assert.IsNotNull(services);
        Assert.IsNotNull(services.GetService<IAppLogger>());
        Assert.IsNotNull(services.GetService<IAppLogReader>());
        Assert.IsNotNull(services.GetService<IWslGlobalConfigService>());
        Assert.IsNotNull(services.GetService<IWslCommandService>());
        Assert.IsNotNull(services.GetService<IWslDistroService>());
        Assert.IsNotNull(services.GetService<IDashboardStatusService>());
        Assert.IsNotNull(services.GetService<IDiagnosticsService>());
        Assert.IsNotNull(services.GetService<DashboardViewModel>());
        Assert.IsNotNull(services.GetService<CommandRunnerViewModel>());
        Assert.IsNotNull(services.GetService<DistroViewModel>());
        Assert.IsNotNull(services.GetService<LogsViewModel>());
        Assert.IsNotNull(services.GetService<SettingsViewModel>());
        Assert.IsNotNull(services.GetService<ShellViewModel>());
        Assert.IsNotNull(services.GetService<WslModuleMarker>());
        Assert.IsNotNull(services.GetService<ConfigurationModuleMarker>());
        Assert.IsNotNull(services.GetService<DiagnosticsModuleMarker>());
    }
}

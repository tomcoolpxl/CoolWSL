using CoolWSL.App.Services;
using CoolWSL.App.ViewModels;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.Settings;

[TestClass]
public sealed class SettingsViewModelTests
{
    [TestMethod]
    public async Task RevertGlobalConfig_RestoresLoadedContent()
    {
        var configService = new StubGlobalConfigService("[wsl2]\nmemory=2GB\n");
        var viewModel = new SettingsViewModel(
            new StubDashboardStatusService(),
            new StubWslDistroService(),
            configService,
            new StubThemePreferenceService(),
            new StubAppLogger());

        await viewModel.RefreshAsync();
        viewModel.UpdateGlobalConfigContent("[wsl2]\nmemory=4GB\n");

        Assert.IsTrue(viewModel.CanRevertGlobalConfig);

        viewModel.RevertGlobalConfig();

        Assert.AreEqual("[wsl2]\nmemory=2GB\n", viewModel.GlobalConfigContent);
        Assert.IsFalse(viewModel.HasGlobalConfigChanges);
    }

    [TestMethod]
    public async Task MalformedGlobalConfig_DisablesSave()
    {
        var viewModel = new SettingsViewModel(
            new StubDashboardStatusService(),
            new StubWslDistroService(),
            new StubGlobalConfigService("[wsl2]\nmemory=2GB\n"),
            new StubThemePreferenceService(),
            new StubAppLogger());

        await viewModel.RefreshAsync();
        viewModel.UpdateGlobalConfigContent("[wsl2]\nmemory=bad\n");

        Assert.IsFalse(viewModel.CanSaveGlobalConfig);
        StringAssert.Contains(viewModel.GlobalConfigValidationText, "Error line");
    }

    [TestMethod]
    public void SelectedThemeIndex_UpdatesThemePreference()
    {
        var themeService = new StubThemePreferenceService();
        var viewModel = new SettingsViewModel(
            new StubDashboardStatusService(),
            new StubWslDistroService(),
            new StubGlobalConfigService("[wsl2]\nmemory=2GB\n"),
            themeService,
            new StubAppLogger());

        viewModel.SelectedThemeIndex = 2;

        Assert.AreEqual(AppThemePreference.Dark, themeService.CurrentTheme);
        Assert.AreEqual(2, viewModel.SelectedThemeIndex);
    }

    private sealed class StubGlobalConfigService : IWslGlobalConfigService
    {
        private readonly string content;

        public StubGlobalConfigService(string content)
        {
            this.content = content;
        }

        public Task<WslGlobalConfigDocument> ReadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WslGlobalConfigDocument(
                @"C:\Users\Test\.wslconfig",
                true,
                content,
                DateTimeOffset.UnixEpoch,
                Validate(content)));

        public WslConfigValidationResult Validate(string content)
        {
            if (content.Contains("bad", StringComparison.OrdinalIgnoreCase))
            {
                return new WslConfigValidationResult(
                [
                    new(WslConfigValidationSeverity.Error, "Setting memory expects a size such as 8GB, 512MB, or 0.", 2),
                ]);
            }

            return WslConfigValidationResult.Empty;
        }

        public Task<WslGlobalConfigSaveResult> SaveAsync(string content, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubDashboardStatusService : IDashboardStatusService
    {
        public Task<DashboardStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardStatusSnapshot(
                new WslEnvironmentStatus(
                    WslAvailability.Available,
                    "WSL is available.",
                    "Ubuntu",
                    2,
                    "6.6.87.2",
                    "2.5.9",
                    "10.0.26100",
                    false,
                    null,
                    null),
                new WslDistroInventory(
                    WslAvailability.Available,
                    [new WslDistro("Ubuntu", WslDistroState.Running, "Running", 2, true)],
                    "Loaded WSL distro inventory.")));
    }

    private sealed class StubWslDistroService : IWslDistroService
    {
        public Task<WslDistroInventory> GetDistroInventoryAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WslEnvironmentStatus> GetEnvironmentStatusAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> OpenDefaultDistroAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> OpenDistroAsync(string distroName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> StartDistroAsync(string distroName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> TerminateDistroAsync(string distroName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> SetDefaultDistroAsync(string distroName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> ShutdownAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> RunInDistroAsync(string distroName, string commandText, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubAppLogger : IAppLogger
    {
        public void LogInfo(string area, string message)
        {
        }
    }

    private sealed class StubThemePreferenceService : IThemePreferenceService
    {
        public event EventHandler? ThemeChanged;

        public AppThemePreference CurrentTheme { get; private set; } = AppThemePreference.System;

        public void SetTheme(AppThemePreference theme)
        {
            CurrentTheme = theme;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

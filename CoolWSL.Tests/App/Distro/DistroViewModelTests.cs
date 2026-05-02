using CoolWSL.App.Services;
using CoolWSL.App.ViewModels;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Models;
using CoolWSL.Diagnostics.Services;
using CoolWSL.Diagnostics.Status;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.Distro;

[TestClass]
public sealed class DistroViewModelTests
{
    [TestMethod]
    public async Task EnsureLoadedAsync_LoadsDistrosAndSelectsDefault()
    {
        var viewModel = CreateViewModel(
            CreateSnapshot("Ubuntu", "Debian"),
            CreateDiagnosticsSnapshot("Ubuntu"));

        await viewModel.EnsureLoadedAsync();

        Assert.IsTrue(viewModel.HasLoaded);
        Assert.AreEqual(2, viewModel.Distros.Count);
        Assert.AreEqual("Ubuntu", viewModel.SelectedDistroName);
        Assert.IsTrue(viewModel.HasSelection);
    }

    [TestMethod]
    public async Task SelectDistroAsync_UpdatesHeaderProperties()
    {
        var viewModel = CreateViewModel(
            CreateSnapshot("Ubuntu", "Debian"),
            CreateDiagnosticsSnapshot("Ubuntu"),
            CreateDiagnosticsSnapshot("Debian"));

        await viewModel.EnsureLoadedAsync();
        await viewModel.SelectDistroAsync("Debian");

        Assert.AreEqual("Debian", viewModel.SelectedDistroName);
        Assert.AreEqual("Debian", viewModel.HeaderName);
    }

    [TestMethod]
    public async Task RefreshAsync_HandlesNoDistrosInstalledState()
    {
        var snapshot = new DashboardStatusSnapshot(
            new WslEnvironmentStatus(
                WslAvailability.Available,
                "WSL is available.",
                null, 2, "6.6.87.2", "2.5.9", "10.0.26100", false, null, null),
            new WslDistroInventory(
                WslAvailability.Available,
                Array.Empty<WslDistro>(),
                "No distros installed."));
        var viewModel = CreateViewModel(snapshot);

        await viewModel.RefreshAsync();

        Assert.IsTrue(viewModel.HasLoaded);
        Assert.IsFalse(viewModel.HasSelection);
        Assert.IsTrue(viewModel.ShowEmptyState);
        Assert.AreEqual(0, viewModel.Distros.Count);
    }

    [TestMethod]
    public async Task TerminateDistroAsync_RequiresSelection()
    {
        var snapshot = new DashboardStatusSnapshot(
            new WslEnvironmentStatus(
                WslAvailability.Available,
                "WSL is available.",
                null, 2, "6.6.87.2", "2.5.9", "10.0.26100", false, null, null),
            new WslDistroInventory(
                WslAvailability.Available,
                Array.Empty<WslDistro>(),
                "No distros installed."));
        var viewModel = CreateViewModel(snapshot);

        await viewModel.RefreshAsync();
        await viewModel.TerminateDistroAsync();

        Assert.AreEqual("Select a distro before running a lifecycle action.", viewModel.ActionStatusText);
    }

    [TestMethod]
    public async Task RefreshAsync_IgnoresSupersededResults()
    {
        var firstStarted = new TaskCompletionSource<object?>();
        var releaseFirst = new TaskCompletionSource<object?>();

        var viewModel = CreateViewModel(
            new SequenceDashboardStatusService(
                async _ =>
                {
                    firstStarted.SetResult(null);
                    await releaseFirst.Task;
                    return CreateSnapshot("Ubuntu");
                },
                _ => Task.FromResult(CreateSnapshot("Debian"))),
            new StubDiagnosticsService(_ => Task.FromResult(CreateDiagnosticsSnapshot("Debian"))));

        var firstRefresh = viewModel.RefreshAsync();
        await firstStarted.Task;

        await viewModel.RefreshAsync();

        releaseFirst.SetResult(null);
        await firstRefresh;

        Assert.AreEqual("Debian", viewModel.SelectedDistroName);
    }

    private static DashboardStatusSnapshot CreateSnapshot(params string[] distroNames)
    {
        var defaultName = distroNames.FirstOrDefault();
        return new(
            new WslEnvironmentStatus(
                WslAvailability.Available,
                "WSL is available.",
                defaultName, 2, "6.6.87.2", "2.5.9", "10.0.26100", false, null, null),
            new WslDistroInventory(
                WslAvailability.Available,
                distroNames.Select((name, index) => new WslDistro(name, WslDistroState.Running, "Running", 2, index == 0)).ToArray(),
                "Loaded WSL distro inventory."));
    }

    private static DiagnosticsSnapshot CreateDiagnosticsSnapshot(string distroName)
    {
        return new(
            new WslEnvironmentStatus(
                WslAvailability.Available,
                "WSL is available.",
                distroName, 2, "6.6.87.2", "2.5.9", "10.0.26100", false, null, null),
            new WslDistroInventory(
                WslAvailability.Available,
                new[] { new WslDistro(distroName, WslDistroState.Running, "Running", 2, true) },
                "Loaded."),
            distroName,
            Array.Empty<DiagnosticResult>());
    }

    private static DistroViewModel CreateViewModel(
        DashboardStatusSnapshot snapshot,
        params DiagnosticsSnapshot[] diagnosticsSnapshots)
    {
        var diagQueue = new Queue<DiagnosticsSnapshot>(diagnosticsSnapshots);
        return CreateViewModel(
            new SequenceDashboardStatusService(_ => Task.FromResult(snapshot)),
            new StubDiagnosticsService(_ =>
            {
                if (diagQueue.Count > 0)
                {
                    return Task.FromResult(diagQueue.Dequeue());
                }

                return Task.FromResult(CreateDiagnosticsSnapshot("fallback"));
            }));
    }

    private static DistroViewModel CreateViewModel(
        IDashboardStatusService dashboardService,
        IDiagnosticsService diagnosticsService)
    {
        var commandRunner = new CommandRunnerViewModel(new StubWslDistroService());
        var diagnosticsVm = new DistroPageDiagnosticsViewModel(diagnosticsService);
        return new DistroViewModel(dashboardService, new StubWslDistroService(), commandRunner, diagnosticsVm);
    }

    private sealed class SequenceDashboardStatusService : IDashboardStatusService
    {
        private readonly Queue<Func<CancellationToken, Task<DashboardStatusSnapshot>>> responses;

        public SequenceDashboardStatusService(params Func<CancellationToken, Task<DashboardStatusSnapshot>>[] responses)
        {
            this.responses = new Queue<Func<CancellationToken, Task<DashboardStatusSnapshot>>>(responses);
        }

        public Task<DashboardStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return responses.Dequeue().Invoke(cancellationToken);
        }
    }

    private sealed class StubDiagnosticsService : IDiagnosticsService
    {
        private readonly Func<string?, Task<DiagnosticsSnapshot>> handler;

        public StubDiagnosticsService(Func<string?, Task<DiagnosticsSnapshot>> handler)
        {
            this.handler = handler;
        }

        public Task<DiagnosticsSnapshot> GetSnapshotAsync(string? selectedDistroName, CancellationToken cancellationToken = default)
            => handler(selectedDistroName);
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
}

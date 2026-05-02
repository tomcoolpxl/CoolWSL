using CoolWSL.App.ViewModels;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.Dashboard;

[TestClass]
public sealed class DashboardViewModelTests
{
    [TestMethod]
    public async Task RefreshAsync_LoadsSnapshotIntoState()
    {
        var viewModel = new DashboardViewModel(
            new SequenceDashboardStatusService(_ => Task.FromResult(CreateSnapshot("Ubuntu", "2.5.9"))),
            new StubWslDistroService());

        await viewModel.RefreshAsync();

        Assert.IsTrue(viewModel.State.HasLoaded);
        Assert.AreEqual("Installed", viewModel.State.AvailabilityLabel);
        Assert.AreEqual("Ubuntu", viewModel.State.Distros.Single().Name);
        Assert.IsFalse(viewModel.State.IsLoading);
    }

    [TestMethod]
    public async Task RefreshAsync_IgnoresSupersededRefreshResults()
    {
        var firstStarted = new TaskCompletionSource<object?>();
        var releaseFirst = new TaskCompletionSource<object?>();

        var viewModel = new DashboardViewModel(
            new SequenceDashboardStatusService(
                async _ =>
                {
                    firstStarted.SetResult(null);
                    await releaseFirst.Task;
                    return CreateSnapshot("Ubuntu", "2.4.0");
                },
                _ => Task.FromResult(CreateSnapshot("Debian", "2.5.0"))),
            new StubWslDistroService());

        var firstRefresh = viewModel.RefreshAsync();
        await firstStarted.Task;

        await viewModel.RefreshAsync();

        releaseFirst.SetResult(null);
        await firstRefresh;

        Assert.AreEqual("Debian", viewModel.State.Distros.Single().Name);
        Assert.AreEqual("2.5.0", viewModel.State.WslVersion);
    }

    [TestMethod]
    public async Task StartDistroAsync_SetsActionStatusAndRefreshesInventory()
    {
        var viewModel = new DashboardViewModel(
            new SequenceDashboardStatusService(
                _ => Task.FromResult(CreateSnapshot("Ubuntu", "2.5.9", isRunning: false)),
                _ => Task.FromResult(CreateSnapshot("Ubuntu", "2.5.9", isRunning: true))),
            new StubWslDistroService
            {
                StartDistroResult = CommandResult.Succeeded(
                    new WslCommand("wsl.exe", new[] { "--distribution", "Ubuntu", "--exec", "/bin/sh", "-lc", ":" }),
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch.AddSeconds(1),
                    string.Empty,
                    string.Empty,
                    0),
            });

        await viewModel.RefreshAsync();
        await viewModel.StartDistroAsync("Ubuntu");

        Assert.AreEqual("Started Ubuntu.", viewModel.ActionStatusText);
        Assert.IsTrue(viewModel.State.Distros.Single().IsRunning);
    }

    private static DashboardStatusSnapshot CreateSnapshot(string distroName, string wslVersion, bool isRunning = true)
    {
        return new(
            new(
                WslAvailability.Available,
                "WSL is available.",
                distroName,
                2,
                "6.6.87.2",
                wslVersion,
                "10.0.26100",
                false,
                null,
                null),
            new(
                WslAvailability.Available,
                new[]
                {
                    new WslDistro(distroName, isRunning ? WslDistroState.Running : WslDistroState.Stopped, isRunning ? "Running" : "Stopped", 2, true),
                },
                "Loaded WSL distro inventory."));
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

    private sealed class StubWslDistroService : IWslDistroService
    {
        public CommandResult? StartDistroResult { get; init; }

        public Task<WslDistroInventory> GetDistroInventoryAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WslEnvironmentStatus> GetEnvironmentStatusAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> OpenDefaultDistroAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> OpenDistroAsync(string distroName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CommandResult> StartDistroAsync(string distroName, CancellationToken cancellationToken = default)
            => Task.FromResult(StartDistroResult ?? CommandResult.Succeeded(new WslCommand("wsl.exe", Array.Empty<string>()), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, string.Empty, string.Empty, 0));

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
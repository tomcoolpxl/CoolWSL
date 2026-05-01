using CoolWSL.App.Services;
using CoolWSL.App.ViewModels;
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
            new RefreshCoordinator());

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
            new RefreshCoordinator());

        var firstRefresh = viewModel.RefreshAsync();
        await firstStarted.Task;

        await viewModel.RefreshAsync();

        releaseFirst.SetResult(null);
        await firstRefresh;

        Assert.AreEqual("Debian", viewModel.State.Distros.Single().Name);
        Assert.AreEqual("2.5.0", viewModel.State.WslVersion);
    }

    private static DashboardStatusSnapshot CreateSnapshot(string distroName, string wslVersion)
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
                    new WslDistro(distroName, WslDistroState.Running, "Running", 2, true),
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
}
using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Models;
using CoolWSL.Diagnostics.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.Diagnostics;

[TestClass]
public sealed class DiagnosticsViewModelTests
{
    [TestMethod]
    public async Task EnsureLoadedAsync_LoadsResultsOnFirstCall()
    {
        var viewModel = CreateViewModel(_ => Task.FromResult(CreateSnapshot("Ubuntu", 3)));

        await viewModel.EnsureLoadedAsync();

        Assert.IsTrue(viewModel.HasLoaded);
        Assert.AreEqual(3, viewModel.Results.Count);
        Assert.AreEqual("Ubuntu", viewModel.SelectedDistroName);
        Assert.IsFalse(viewModel.IsLoading);
    }

    [TestMethod]
    public async Task SelectDistroAsync_RefreshesWithSelectedDistro()
    {
        string? lastRequestedDistro = null;
        var viewModel = CreateViewModel(distroName =>
        {
            lastRequestedDistro = distroName;
            return Task.FromResult(CreateSnapshot(distroName ?? "Ubuntu", 2));
        });

        await viewModel.EnsureLoadedAsync();
        await viewModel.SelectDistroAsync("Debian");

        Assert.AreEqual("Debian", lastRequestedDistro);
    }

    [TestMethod]
    public async Task RefreshAsync_BuildsWarningFromErrors()
    {
        var results = new List<DiagnosticResult>
        {
            new("test-pass", "Pass Check", DiagnosticSeverity.Success, "All good.", string.Empty, string.Empty),
            new("test-warn", "Warn Check", DiagnosticSeverity.Warning, "Something is off.", string.Empty, string.Empty),
            new("test-error", "Error Check", DiagnosticSeverity.Error, "Something is broken.", string.Empty, string.Empty),
        };
        var snapshot = new DiagnosticsSnapshot(
            CreateEnvironmentStatus("Ubuntu"),
            CreateInventory("Ubuntu"),
            "Ubuntu",
            results);
        var viewModel = CreateViewModel(_ => Task.FromResult(snapshot));

        await viewModel.RefreshAsync();

        Assert.IsTrue(viewModel.HasWarning);
        Assert.IsTrue(viewModel.WarningText.Contains("Something is off."));
        Assert.IsTrue(viewModel.WarningText.Contains("Something is broken."));
    }

    [TestMethod]
    public async Task RefreshAsync_HandlesExceptionGracefully()
    {
        var viewModel = CreateViewModel(_ => throw new InvalidOperationException("Test error"));

        await viewModel.RefreshAsync();

        Assert.IsTrue(viewModel.HasLoaded);
        Assert.AreEqual(0, viewModel.Results.Count);
        Assert.AreEqual("CoolWSL could not load diagnostics.", viewModel.SummaryText);
        Assert.AreEqual("Test error", viewModel.WarningText);
        Assert.IsFalse(viewModel.IsLoading);
    }

    private static DiagnosticsViewModel CreateViewModel(Func<string?, Task<DiagnosticsSnapshot>> handler)
        => new(new StubDiagnosticsService(handler));

    private static DiagnosticsSnapshot CreateSnapshot(string distroName, int resultCount)
    {
        var results = Enumerable.Range(0, resultCount)
            .Select(i => new DiagnosticResult($"check-{i}", $"Check {i}", DiagnosticSeverity.Success, $"Check {i} passed.", string.Empty, string.Empty))
            .ToList();

        return new(
            CreateEnvironmentStatus(distroName),
            CreateInventory(distroName),
            distroName,
            results);
    }

    private static WslEnvironmentStatus CreateEnvironmentStatus(string defaultDistroName)
        => new(WslAvailability.Available, "WSL is available.", defaultDistroName, 2, "6.6.87.2", "2.5.9", "10.0.26100", false, null, null);

    private static WslDistroInventory CreateInventory(params string[] distroNames)
        => new(
            WslAvailability.Available,
            distroNames.Select((name, index) => new WslDistro(name, WslDistroState.Running, "Running", 2, index == 0)).ToArray(),
            "Loaded.");

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
}

using CoolWSL.App.ViewModels;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Core.Models.Configuration;
using CoolWSL.Diagnostics.Status;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CoolWSL.Tests.App.Distro;

[TestClass]
public sealed class DistroSettingsViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_IgnoresSupersededResults()
    {
        var firstStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var configService = new SequenceDistroConfigService(
            async (distroName, cancellationToken) =>
            {
                firstStarted.SetResult(null);
                await releaseFirst.Task.WaitAsync(cancellationToken);
                return CreateDocument(distroName, "[boot]\nsystemd=true\n");
            },
            (distroName, _) => Task.FromResult(CreateDocument(distroName, "[user]\ndefault=debian\n")));
        var viewModel = new DistroSettingsViewModel(
            configService,
            new StubDashboardStatusService(CreateSnapshot("Ubuntu", "Debian")),
            new StubGlobalConfigService("[wsl2]\nmemory=4GB\n"));

        viewModel.SetSelectedDistro("Ubuntu");
        var firstLoadTask = viewModel.LoadAsync();
        await firstStarted.Task;

        viewModel.SetSelectedDistro("Debian");
        await viewModel.LoadAsync();

        releaseFirst.SetResult(null);
        await firstLoadTask;

        Assert.AreEqual("[user]\ndefault=debian\n", viewModel.RawText);
        Assert.AreEqual("Global .wslconfig: memory 4GB, networking NAT, GUI apps on.", viewModel.GlobalWslSummary);
        Assert.IsTrue(viewModel.StatusMessage.StartsWith("Loaded at ", StringComparison.Ordinal));
        Assert.IsFalse(viewModel.IsLoading);
        Assert.AreEqual(1, configService.CancelledReadCount);
        Assert.AreEqual("debian", viewModel.Rows.Single(row => row.KeyId == "user.default").Value);
    }

    private static DashboardStatusSnapshot CreateSnapshot(params string[] distroNames)
    {
        var defaultName = distroNames.FirstOrDefault();
        return new(
            new WslEnvironmentStatus(
                WslAvailability.Available,
                "WSL is available.",
                defaultName,
                2,
                "6.6.87.2",
                "2.5.9",
                "10.0.26100",
                false,
                null,
                null),
            new WslDistroInventory(
                WslAvailability.Available,
                distroNames.Select((name, index) => new WslDistro(name, WslDistroState.Running, "Running", 2, index == 0)).ToArray(),
                "Loaded WSL distro inventory."));
    }

    private static WslDistroConfigDocument CreateDocument(string distroName, string content)
        => new(
            distroName,
            IniParser.Parse(content),
            content,
            true,
            DateTimeOffset.UnixEpoch,
            WslConfigValidationResult.Empty);

    private sealed class SequenceDistroConfigService : IWslDistroConfigService
    {
        private readonly Queue<Func<string, CancellationToken, Task<WslDistroConfigDocument>>> readHandlers;

        public SequenceDistroConfigService(params Func<string, CancellationToken, Task<WslDistroConfigDocument>>[] readHandlers)
        {
            this.readHandlers = new Queue<Func<string, CancellationToken, Task<WslDistroConfigDocument>>>(readHandlers);
        }

        public int CancelledReadCount { get; private set; }

        public async Task<WslDistroConfigDocument> ReadAsync(string distroName, CancellationToken cancellationToken = default)
        {
            try
            {
                return await readHandlers.Dequeue().Invoke(distroName, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                CancelledReadCount++;
                throw;
            }
        }

        public WslConfigValidationResult Validate(IniDocument document, WslDistroCapabilityContext capabilities)
            => WslConfigValidationResult.Empty;

        public Task<WslDistroConfigSaveResult> SaveAsync(string distroName, IniDocument document, WslDistroCapabilityContext capabilities, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<WslConfigProbeResult>> ProbeAsync(string distroName, IniDocument document, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WslDistroConfigDeleteResult> RestoreDefaultsAsync(string distroName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubDashboardStatusService : IDashboardStatusService
    {
        private readonly DashboardStatusSnapshot snapshot;

        public StubDashboardStatusService(DashboardStatusSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public Task<DashboardStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(snapshot);
    }

    private sealed class StubGlobalConfigService : IWslGlobalConfigService
    {
        private readonly string content;

        public StubGlobalConfigService(string content)
        {
            this.content = content;
        }

        public Task<WslGlobalConfigDocument> ReadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WslGlobalConfigDocument(".wslconfig", true, content, DateTimeOffset.UnixEpoch, WslConfigValidationResult.Empty));

        public WslConfigValidationResult Validate(string content)
            => WslConfigValidationResult.Empty;

        public Task<WslGlobalConfigSaveResult> SaveAsync(string content, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
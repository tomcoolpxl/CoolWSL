using CoolWSL.App.ViewModels;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.CommandRunner;

[TestClass]
public sealed class CommandRunnerViewModelTests
{
    [TestMethod]
    public async Task RunAsync_SuccessStoresOutputAndSessionHistoryPerDistro()
    {
        var service = new StubWslDistroService
        {
            RunHandler = (_, _, _, _) => Task.FromResult(CreateSucceededResult("echo hello", "hello")),
        };
        var viewModel = new CommandRunnerViewModel(service);

        viewModel.SetSelectedDistro("Ubuntu");
        viewModel.CommandText = "echo hello";

        await viewModel.RunAsync();

        Assert.AreEqual("Command succeeded in Ubuntu.", viewModel.StatusText);
        Assert.AreEqual("hello", viewModel.StandardOutput);
        Assert.AreEqual(1, viewModel.History.Count);

        viewModel.SetSelectedDistro("Debian");
        Assert.AreEqual(0, viewModel.History.Count);

        viewModel.SetSelectedDistro("Ubuntu");
        Assert.AreEqual(1, viewModel.History.Count);
    }

    [TestMethod]
    public async Task RunAsync_TimedOutSurfacesErrorSummaryAndHistory()
    {
        var result = CommandResult.TimedOut(
            new WslCommand("wsl.exe", new[] { "--distribution", "Ubuntu", "--exec", "/bin/sh", "-lc", "sleep 30" }),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(30),
            string.Empty,
            string.Empty,
            null,
            new WslCommandError(WslErrorKind.Timeout, "The WSL command timed out after 30 seconds."));
        var service = new StubWslDistroService
        {
            RunHandler = (_, _, _, _) => Task.FromResult(result),
        };
        var viewModel = new CommandRunnerViewModel(service);

        viewModel.SetSelectedDistro("Ubuntu");
        viewModel.CommandText = "sleep 30";

        await viewModel.RunAsync();

        Assert.AreEqual("The WSL command timed out after 30 seconds.", viewModel.StatusText);
        Assert.AreEqual(CommandExecutionStatus.TimedOut, viewModel.History.Single().Status);
    }

    [TestMethod]
    public async Task Cancel_CancelsActiveCommand()
    {
        var started = new TaskCompletionSource<object?>();
        var service = new StubWslDistroService
        {
            RunHandler = async (_, commandText, _, cancellationToken) =>
            {
                started.SetResult(null);
                var cancelled = new TaskCompletionSource<object?>();
                using var registration = cancellationToken.Register(() => cancelled.TrySetResult(null));
                await cancelled.Task;

                return CommandResult.Cancelled(
                    new WslCommand("wsl.exe", new[] { "--distribution", "Ubuntu", "--exec", "/bin/sh", "-lc", commandText }),
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch.AddSeconds(1),
                    string.Empty,
                    string.Empty,
                    null,
                    new WslCommandError(WslErrorKind.Cancelled, "The WSL command was cancelled."));
            },
        };
        var viewModel = new CommandRunnerViewModel(service);

        viewModel.SetSelectedDistro("Ubuntu");
        viewModel.CommandText = "sleep 5";

        var runTask = viewModel.RunAsync();
        await started.Task;

        viewModel.Cancel();
        await runTask;

        Assert.IsFalse(viewModel.IsRunning);
        Assert.AreEqual("The WSL command was cancelled.", viewModel.StatusText);
        Assert.AreEqual(CommandExecutionStatus.Cancelled, viewModel.History.Single().Status);
    }

    private static CommandResult CreateSucceededResult(string commandText, string output)
    {
        return CommandResult.Succeeded(
            new WslCommand("wsl.exe", new[] { "--distribution", "Ubuntu", "--exec", "/bin/sh", "-lc", commandText }),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1),
            output,
            string.Empty,
            0);
    }

    private sealed class StubWslDistroService : IWslDistroService
    {
        public Func<string, string, TimeSpan?, CancellationToken, Task<CommandResult>>? RunHandler { get; init; }

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
            => RunHandler?.Invoke(distroName, commandText, timeout, cancellationToken)
                ?? throw new NotSupportedException();
    }
}
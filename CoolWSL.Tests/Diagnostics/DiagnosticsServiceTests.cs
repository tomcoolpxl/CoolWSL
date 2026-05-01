using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Mappers;
using CoolWSL.Diagnostics.Models;
using CoolWSL.Diagnostics.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Diagnostics;

[TestClass]
public sealed class DiagnosticsServiceTests
{
    [TestMethod]
    public async Task GetSnapshotAsync_UsesRequestedDistroAndMapsHealthyResults()
    {
        var distroService = new StubWslDistroService(
            CreateEnvironmentStatus(defaultDistroName: "Ubuntu"),
            CreateInventory("Ubuntu", "Debian"),
            CreateSucceededInDistroResult("Ubuntu", "getent hosts learn.microsoft.com", "13.107.246.57 learn.microsoft.com"),
            CreateSucceededInDistroResult("Ubuntu", "curl -I -sS --max-time 10 https://learn.microsoft.com >/dev/null", string.Empty));
        var commandService = new StubWslCommandService(new Dictionary<string, CommandResult>(StringComparer.Ordinal)
        {
            ["wsl.exe --status"] = CommandResult.Succeeded(new WslCommand("wsl.exe", new[] { "--status" }), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(1), "Default Distribution: Ubuntu", string.Empty, 0),
            ["wsl.exe --version"] = CommandResult.Succeeded(new WslCommand("wsl.exe", new[] { "--version" }), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(1), "WSL version: 2.5.9", string.Empty, 0),
            ["wsl.exe --list --verbose"] = CommandResult.Succeeded(new WslCommand("wsl.exe", new[] { "--list", "--verbose" }), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(1), "Ubuntu\nDebian", string.Empty, 0),
        });
        var service = new DiagnosticsService(commandService, distroService, new DiagnosticSummaryMapper());

        var snapshot = await service.GetSnapshotAsync("Debian");

        Assert.AreEqual("Debian", snapshot.SelectedDistroName);
        Assert.AreEqual(7, snapshot.Results.Count);
        Assert.AreEqual(DiagnosticSeverity.Success, snapshot.Results.Single(result => result.Id == "dns-resolution").Severity);
        Assert.AreEqual(DiagnosticSeverity.Success, snapshot.Results.Single(result => result.Id == "internet-connectivity").Severity);
        CollectionAssert.AreEqual(new[] { "Debian", "Debian" }, distroService.RunRequests.Select(request => request.DistroName).ToArray());
    }

    [TestMethod]
    public async Task GetSnapshotAsync_FallsBackToDefaultDistroAndMapsUnsupportedDnsToolAsWarning()
    {
        var unsupportedDnsResult = CommandResult.Failed(
            new WslCommand("wsl.exe", new[] { "--distribution", "Ubuntu" }),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1),
            string.Empty,
            "No supported DNS test tool was found.",
            127,
            new WslCommandError(WslErrorKind.Unknown, "The WSL command failed."));
        var failedInternetResult = CommandResult.Failed(
            new WslCommand("wsl.exe", new[] { "--distribution", "Ubuntu" }),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1),
            string.Empty,
            "ping: connect: Network is unreachable",
            1,
            new WslCommandError(WslErrorKind.Unknown, "The WSL command failed.", "Review the raw output for the failing probe."));
        var distroService = new StubWslDistroService(
            CreateEnvironmentStatus(defaultDistroName: "Ubuntu"),
            CreateInventory("Ubuntu"),
            unsupportedDnsResult,
            failedInternetResult);
        var commandService = new StubWslCommandService(new Dictionary<string, CommandResult>(StringComparer.Ordinal)
        {
            ["wsl.exe --status"] = CommandResult.Succeeded(new WslCommand("wsl.exe", new[] { "--status" }), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(1), "Default Distribution: Ubuntu", string.Empty, 0),
            ["wsl.exe --version"] = CommandResult.Failed(new WslCommand("wsl.exe", new[] { "--version" }), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(1), string.Empty, "Invalid command line option: --version", 1, new WslCommandError(WslErrorKind.UnsupportedFeature, "This WSL installation does not support that command.")),
            ["wsl.exe --list --verbose"] = CommandResult.Succeeded(new WslCommand("wsl.exe", new[] { "--list", "--verbose" }), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(1), "Ubuntu", string.Empty, 0),
        });
        var service = new DiagnosticsService(commandService, distroService, new DiagnosticSummaryMapper());

        var snapshot = await service.GetSnapshotAsync("Missing");

        Assert.AreEqual("Ubuntu", snapshot.SelectedDistroName);
        Assert.AreEqual(DiagnosticSeverity.Warning, snapshot.Results.Single(result => result.Id == "dns-resolution").Severity);
        Assert.AreEqual(DiagnosticSeverity.Error, snapshot.Results.Single(result => result.Id == "internet-connectivity").Severity);
        Assert.AreEqual(DiagnosticSeverity.Warning, snapshot.Results.Single(result => result.Id == "wsl-version").Severity);
    }

    private static WslEnvironmentStatus CreateEnvironmentStatus(string? defaultDistroName)
        => new(
            WslAvailability.Available,
            "WSL is available.",
            defaultDistroName,
            2,
            "6.6.87.2",
            "2.5.9",
            "10.0.26100",
            false,
            null,
            null);

    private static WslDistroInventory CreateInventory(params string[] distroNames)
        => new(
            WslAvailability.Available,
            distroNames.Select((name, index) => new WslDistro(name, WslDistroState.Running, "Running", 2, index == 0)).ToArray(),
            "Loaded WSL distro inventory.");

    private static CommandResult CreateSucceededInDistroResult(string distroName, string commandText, string stdout)
        => CommandResult.Succeeded(
            new WslCommand("wsl.exe", new[] { "--distribution", distroName, "--exec", "/bin/sh", "-lc", commandText }),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1),
            stdout,
            string.Empty,
            0);

    private sealed class StubWslCommandService : IWslCommandService
    {
        private readonly IReadOnlyDictionary<string, CommandResult> results;

        public StubWslCommandService(IReadOnlyDictionary<string, CommandResult> results)
        {
            this.results = results;
        }

        public Task<CommandResult> ExecuteAsync(WslCommand command, CancellationToken cancellationToken = default)
        {
            if (!results.TryGetValue(command.CommandText, out var result))
            {
                throw new AssertFailedException($"No stubbed command result exists for {command.CommandText}.");
            }

            return Task.FromResult(result);
        }
    }

    private sealed class StubWslDistroService : IWslDistroService
    {
        private readonly Queue<CommandResult> inDistroResults;

        public StubWslDistroService(WslEnvironmentStatus environmentStatus, WslDistroInventory inventory, params CommandResult[] inDistroResults)
        {
            EnvironmentStatus = environmentStatus;
            Inventory = inventory;
            this.inDistroResults = new Queue<CommandResult>(inDistroResults);
        }

        public List<(string DistroName, string CommandText)> RunRequests { get; } = [];

        public WslEnvironmentStatus EnvironmentStatus { get; }

        public WslDistroInventory Inventory { get; }

        public Task<WslDistroInventory> GetDistroInventoryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Inventory);

        public Task<WslEnvironmentStatus> GetEnvironmentStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(EnvironmentStatus);

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
        {
            RunRequests.Add((distroName, commandText));

            if (inDistroResults.Count == 0)
            {
                throw new AssertFailedException("No stubbed in-distro command results remain.");
            }

            return Task.FromResult(inDistroResults.Dequeue());
        }
    }
}
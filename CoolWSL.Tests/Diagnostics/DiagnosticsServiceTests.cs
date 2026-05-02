using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Mappers;
using CoolWSL.Diagnostics.Models;
using CoolWSL.Diagnostics.Services;
using CoolWSL.Wsl.Errors;
using CoolWSL.Wsl.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Diagnostics;

[TestClass]
public sealed class DiagnosticsServiceTests
{
    [TestMethod]
    public async Task GetSnapshotAsync_UsesRequestedDistroAndMapsHealthyResults()
    {
        var commandService = new StubWslCommandService(new Dictionary<string, CommandResult>(StringComparer.Ordinal)
        {
            ["wsl.exe --status"] = CreateSucceeded("--status", "Default Distribution: Ubuntu\nDefault Version: 2"),
            ["wsl.exe --version"] = CreateSucceeded("--version", "WSL version: 2.5.9\nKernel version: 6.6.87.2"),
            ["wsl.exe --list --verbose"] = CreateSucceeded("--list --verbose", "  NAME      STATE           VERSION\n* Ubuntu    Running         2\n  Debian    Running         2"),
            ["wsl.exe --list --running --quiet"] = CreateSucceeded("--list --running --quiet", "Ubuntu\nDebian\n"),
        });
        commandService.DistroCommandResult = CreateSucceeded("probe", string.Empty);
        var service = CreateService(commandService);

        var snapshot = await service.GetSnapshotAsync("Debian");

        Assert.AreEqual("Debian", snapshot.SelectedDistroName);
        Assert.AreEqual(7, snapshot.Results.Count);
        Assert.AreEqual(DiagnosticSeverity.Success, snapshot.Results.Single(result => result.Id == "dns-resolution").Severity);
        Assert.AreEqual(DiagnosticSeverity.Success, snapshot.Results.Single(result => result.Id == "internet-connectivity").Severity);
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

        var commandService = new StubWslCommandService(new Dictionary<string, CommandResult>(StringComparer.Ordinal)
        {
            ["wsl.exe --status"] = CreateSucceeded("--status", "Default Distribution: Ubuntu\nDefault Version: 2"),
            ["wsl.exe --version"] = CommandResult.Failed(
                new WslCommand("wsl.exe", new[] { "--version" }),
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch.AddSeconds(1),
                string.Empty,
                "Invalid command line option: --version",
                1,
                new WslCommandError(WslErrorKind.UnsupportedFeature, "This WSL installation does not support that command.")),
            ["wsl.exe --list --verbose"] = CreateSucceeded("--list --verbose", "  NAME      STATE           VERSION\n* Ubuntu    Running         2"),
            ["wsl.exe --list --running --quiet"] = CreateSucceeded("--list --running --quiet", "Ubuntu\n"),
        });
        commandService.DistroCommandResults = new Queue<CommandResult>(new[] { unsupportedDnsResult, failedInternetResult });
        var service = CreateService(commandService);

        var snapshot = await service.GetSnapshotAsync("Missing");

        Assert.AreEqual("Ubuntu", snapshot.SelectedDistroName);
        Assert.AreEqual(DiagnosticSeverity.Warning, snapshot.Results.Single(result => result.Id == "dns-resolution").Severity);
        Assert.AreEqual(DiagnosticSeverity.Error, snapshot.Results.Single(result => result.Id == "internet-connectivity").Severity);
        Assert.AreEqual(DiagnosticSeverity.Warning, snapshot.Results.Single(result => result.Id == "wsl-version").Severity);
    }

    [TestMethod]
    public async Task GetSnapshotAsync_InfersLocalizedStateForSelectedDistroInventory()
    {
        var commandService = new StubWslCommandService(new Dictionary<string, CommandResult>(StringComparer.Ordinal)
        {
            ["wsl.exe --status"] = CreateSucceeded("--status", "Default Distribution: Ubuntu 22.04\nDefault Version: 2"),
            ["wsl.exe --version"] = CreateSucceeded("--version", "WSL version: 2.5.9\nKernel version: 6.6.87.2"),
            ["wsl.exe --list --verbose"] = CreateSucceeded("--list --verbose", "NOM              ETAT            VERSION\n* Ubuntu 22.04   En cours        2\n  Debian         Arrete          1\n"),
            ["wsl.exe --list --running --quiet"] = CreateSucceeded("--list --running --quiet", "Ubuntu 22.04\n"),
        });
        commandService.DistroCommandResult = CreateSucceeded("probe", string.Empty);
        var service = CreateService(commandService);

        var snapshot = await service.GetSnapshotAsync("Ubuntu 22.04");

        Assert.AreEqual(WslDistroState.Running, snapshot.DistroInventory.Distros.Single(distro => distro.Name == "Ubuntu 22.04").State);
        Assert.AreEqual(WslDistroState.Stopped, snapshot.DistroInventory.Distros.Single(distro => distro.Name == "Debian").State);
    }

    private static DiagnosticsService CreateService(StubWslCommandService commandService)
        => new(commandService, new WslListParser(), new WslStatusParser(), new WslErrorMapper(), new DiagnosticSummaryMapper());

    private static CommandResult CreateSucceeded(string args, string stdout)
        => CommandResult.Succeeded(
            new WslCommand("wsl.exe", args.Split(' ')),
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

        public CommandResult? DistroCommandResult { get; set; }

        public Queue<CommandResult>? DistroCommandResults { get; set; }

        public Task<CommandResult> ExecuteAsync(WslCommand command, CancellationToken cancellationToken = default)
        {
            if (results.TryGetValue(command.CommandText, out var result))
            {
                return Task.FromResult(result);
            }

            if (command.Arguments.Contains("--distribution"))
            {
                if (DistroCommandResults is { Count: > 0 })
                {
                    return Task.FromResult(DistroCommandResults.Dequeue());
                }

                if (DistroCommandResult is not null)
                {
                    return Task.FromResult(DistroCommandResult);
                }
            }

            throw new AssertFailedException($"No stubbed result exists for {command.CommandText}.");
        }

        public Task<CommandResult> ExecuteWithStdinAsync(WslCommand command, string stdin, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

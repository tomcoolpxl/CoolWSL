using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Wsl.Parsing;
using CoolWSL.Wsl.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Wsl;

[TestClass]
public sealed class WslDistroServiceTests
{
    [TestMethod]
    public async Task GetEnvironmentStatusAsync_UsesDegradedModeWhenVersionCommandIsUnsupported()
    {
        var commandService = new StubWslCommandService(
            new Dictionary<string, CommandResult>(StringComparer.Ordinal)
            {
                ["wsl.exe --status"] = CommandResult.Succeeded(
                    new WslCommand("wsl.exe", new[] { "--status" }),
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch.AddSeconds(1),
                    "Default Distribution: Ubuntu\nDefault Version: 2\nKernel version: 5.15.153.1-2",
                    string.Empty,
                    0),
                ["wsl.exe --version"] = CommandResult.Failed(
                    new WslCommand("wsl.exe", new[] { "--version" }),
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch.AddSeconds(1),
                    string.Empty,
                    "Invalid command line option: --version",
                    1,
                    new WslCommandError(WslErrorKind.UnsupportedFeature, "This WSL installation does not support that command.")),
            });
        var service = new WslDistroService(commandService, new WslListParser(), new WslStatusParser());

        var result = await service.GetEnvironmentStatusAsync();

        Assert.AreEqual(WslAvailability.Available, result.Availability);
        Assert.AreEqual("Ubuntu", result.DefaultDistroName);
        Assert.AreEqual(2, result.DefaultWslVersion);
        Assert.IsTrue(result.IsDegraded);
        Assert.IsNull(result.WslVersion);
    }

    [TestMethod]
    public async Task GetDistroInventoryAsync_ReturnsEmptyInventoryForNoDistributions()
    {
        var commandService = new StubWslCommandService(
            new Dictionary<string, CommandResult>(StringComparer.Ordinal)
            {
                ["wsl.exe --list --verbose"] = CommandResult.Succeeded(
                    new WslCommand("wsl.exe", new[] { "--list", "--verbose" }),
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch.AddSeconds(1),
                    "Windows Subsystem for Linux has no installed distributions.",
                    string.Empty,
                    0),
            });
        var service = new WslDistroService(commandService, new WslListParser(), new WslStatusParser());

        var result = await service.GetDistroInventoryAsync();

        Assert.AreEqual(WslAvailability.Available, result.Availability);
        Assert.AreEqual(0, result.Distros.Count);
        Assert.IsFalse(result.IsDegraded);
    }

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
                throw new AssertFailedException($"No stubbed result exists for {command.CommandText}.");
            }

            return Task.FromResult(result);
        }
    }
}
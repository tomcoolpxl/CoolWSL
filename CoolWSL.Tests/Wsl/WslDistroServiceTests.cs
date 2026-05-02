using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Services;
using CoolWSL.Core.Models;
using CoolWSL.Wsl.Commands;
using CoolWSL.Wsl.Errors;
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
                var service = CreateService(commandService);

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
        var service = CreateService(commandService);

        var result = await service.GetDistroInventoryAsync();

        Assert.AreEqual(WslAvailability.Available, result.Availability);
        Assert.AreEqual(0, result.Distros.Count);
        Assert.IsFalse(result.IsDegraded);
    }

    [TestMethod]
    public async Task TerminateDistroAsync_RunsTerminateCommand()
    {
        var command = WslCommandFactory.CreateTerminateDistroCommand("Ubuntu Dev");
        var expectedResult = CommandResult.Succeeded(
            command,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1),
            string.Empty,
            string.Empty,
            0);
        var commandService = new StubWslCommandService(
            new Dictionary<string, CommandResult>(StringComparer.Ordinal)
            {
                [command.CommandText] = expectedResult,
            });
        var service = CreateService(commandService);

        var result = await service.TerminateDistroAsync("Ubuntu Dev");

        Assert.AreSame(expectedResult, result);
    }

    [TestMethod]
    public async Task RunInDistroAsync_RunsShellCommandWithSelectedTimeout()
    {
        var timeout = TimeSpan.FromSeconds(45);
        var command = WslCommandFactory.CreateRunInDistroCommand("Ubuntu Dev", "echo hello", timeout);
        var expectedResult = CommandResult.Succeeded(
            command,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1),
            "hello",
            string.Empty,
            0);
        var commandService = new StubWslCommandService(
            new Dictionary<string, CommandResult>(StringComparer.Ordinal)
            {
                [command.CommandText] = expectedResult,
            });
        var service = CreateService(commandService);

        var result = await service.RunInDistroAsync("Ubuntu Dev", "echo hello", timeout);

        Assert.AreSame(expectedResult, result);
    }

    [TestMethod]
    public void CreateOpenDefaultDistroCommand_BuildsExpectedCommand()
    {
        var command = WslCommandFactory.CreateOpenDefaultDistroCommand();

        Assert.AreEqual("wsl.exe", command.FileName);
        Assert.AreEqual(0, command.Arguments.Count);
    }

    [TestMethod]
    public void CreateOpenDistroCommand_BuildsExpectedCommand()
    {
        var command = WslCommandFactory.CreateOpenDistroCommand("Ubuntu Dev");

        Assert.AreEqual("wsl.exe", command.FileName);
        Assert.IsTrue(command.Arguments.Contains("--distribution"));
        Assert.IsTrue(command.Arguments.Contains("Ubuntu Dev"));
    }

    private static WslDistroService CreateService(IWslCommandService commandService)
        => new(commandService, new WslErrorMapper(), new WslListParser(), new WslStatusParser(), new NullAppLogger(), TimeProvider.System);

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
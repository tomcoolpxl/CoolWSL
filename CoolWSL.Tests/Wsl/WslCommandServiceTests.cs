using CoolWSL.Core.Models;
using CoolWSL.Core.Services;
using CoolWSL.Wsl.Errors;
using CoolWSL.Wsl.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Wsl;

[TestClass]
public sealed class WslCommandServiceTests
{
    private static readonly TimeProvider TimeProvider = TimeProvider.System;

    [TestMethod]
    public async Task ExecuteAsync_CapturesOutputAndExitCode()
    {
        var service = CreateService();
        var command = new WslCommand(
            "cmd.exe",
            new[] { "/d", "/c", "echo hello & echo problem 1>&2 & exit 7" },
            TimeSpan.FromSeconds(10));

        var result = await service.ExecuteAsync(command);

        Assert.AreEqual(CommandExecutionStatus.Failed, result.Status);
        Assert.AreEqual(7, result.ExitCode);
        StringAssert.Contains(result.StandardOutput, "hello");
        StringAssert.Contains(result.StandardError, "problem");
    }

    [TestMethod]
    public async Task ExecuteAsync_TimesOutLongRunningProcess()
    {
        var service = CreateService();
        var command = new WslCommand(
            "cmd.exe",
            new[] { "/d", "/c", "ping 127.0.0.1 -n 20 > nul" },
            TimeSpan.FromMilliseconds(300));

        var result = await service.ExecuteAsync(command);

        Assert.AreEqual(CommandExecutionStatus.TimedOut, result.Status);
        Assert.AreEqual(WslErrorKind.Timeout, result.Error?.Kind);
    }

    [TestMethod]
    public async Task ExecuteAsync_CancelsLongRunningProcess()
    {
        var service = CreateService();
        var command = new WslCommand(
            "cmd.exe",
            new[] { "/d", "/c", "ping 127.0.0.1 -n 20 > nul" },
            TimeSpan.FromSeconds(10));
        using var cancellationTokenSource = new CancellationTokenSource(millisecondsDelay: 300);

        var result = await service.ExecuteAsync(command, cancellationTokenSource.Token);

        Assert.AreEqual(CommandExecutionStatus.Cancelled, result.Status);
        Assert.AreEqual(WslErrorKind.Cancelled, result.Error?.Kind);
    }

    private static WslCommandService CreateService()
        => new(new WslErrorMapper(), new NullAppLogger(), TimeProvider);
}
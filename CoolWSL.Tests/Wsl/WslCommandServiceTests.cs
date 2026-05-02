using CoolWSL.Core.Models;
using CoolWSL.Core.Services;
using CoolWSL.Wsl.Errors;
using CoolWSL.Wsl.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

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

    [TestMethod]
    public async Task ExecuteAsync_UsesConfiguredUnicodeEncodingForRedirectedOutput()
    {
        var service = CreateService();
        var command = new WslCommand(
            "pwsh",
            [
                "-NoProfile",
                "-Command",
                "[Console]::OutputEncoding = [System.Text.Encoding]::Unicode; [Console]::Out.Write('Default Distribution: archlinux`r`nDefault Version: 2`r`n')"
            ],
            TimeSpan.FromSeconds(10),
            standardOutputEncoding: Encoding.Unicode,
            standardErrorEncoding: Encoding.Unicode);

        var result = await service.ExecuteAsync(command);

        Assert.AreEqual(CommandExecutionStatus.Succeeded, result.Status);
        StringAssert.Contains(result.StandardOutput, "Default Distribution: archlinux");
        StringAssert.Contains(result.StandardOutput, "Default Version: 2");
        Assert.IsFalse(result.StandardOutput.Contains('\0'));
    }

    [TestMethod]
    public async Task ExecuteWithStdinAsync_ReadsRedirectedOutputBeforeLargeStdinCompletes()
    {
        var service = CreateService();
        var line = new string('a', 256);
        var stdin = string.Concat(Enumerable.Repeat(line + "\n", 12000));
        var command = new WslCommand(
            "pwsh",
            new[]
            {
                "-NoProfile",
                "-Command",
                "$reader = [Console]::In; while (($line = $reader.ReadLine()) -ne $null) { [Console]::Out.WriteLine($line) }"
            },
            TimeSpan.FromSeconds(10));

        var result = await service.ExecuteWithStdinAsync(command, stdin);

        Assert.AreEqual(CommandExecutionStatus.Succeeded, result.Status);
        Assert.AreEqual(string.Empty, result.StandardError);
        Assert.IsTrue(result.StandardOutput.Length >= stdin.Length);
    }

    private static WslCommandService CreateService()
        => new(new WslErrorMapper(), new NullAppLogger(), TimeProvider);
}
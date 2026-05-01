using CoolWSL.Core.Models;

namespace CoolWSL.Wsl.Commands;

public static class WslCommandFactory
{
    private static readonly TimeSpan DefaultQueryTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DefaultMutationTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(30);

    public static WslCommand CreateListVerboseCommand(TimeSpan? timeout = null)
        => CreateQueryCommand("List registered WSL distributions", timeout, "--list", "--verbose");

    public static WslCommand CreateStatusCommand(TimeSpan? timeout = null)
        => CreateQueryCommand("Read WSL status", timeout, "--status");

    public static WslCommand CreateVersionCommand(TimeSpan? timeout = null)
        => CreateQueryCommand("Read WSL version details", timeout, "--version");

    public static WslCommand CreateOpenDefaultDistroCommand()
        => new("wsl.exe", Array.Empty<string>(), null, "Open the default WSL distribution in a terminal");

    public static WslCommand CreateOpenDistroCommand(string distroName, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distroName);

        return CreateMutationCommand("Open a WSL distribution", timeout, "--distribution", distroName);
    }

    public static WslCommand CreateStartDistroCommand(string distroName, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distroName);

        return CreateCommandCommand(
            "Start a WSL distribution",
            distroName,
            ":",
            timeout);
    }

    public static WslCommand CreateTerminateDistroCommand(string distroName, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distroName);

        return CreateMutationCommand("Terminate a WSL distribution", timeout, "--terminate", distroName);
    }

    public static WslCommand CreateSetDefaultDistroCommand(string distroName, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distroName);

        return CreateMutationCommand("Set the default WSL distribution", timeout, "--set-default", distroName);
    }

    public static WslCommand CreateShutdownCommand(TimeSpan? timeout = null)
        => CreateMutationCommand("Shut down all WSL distributions", timeout, "--shutdown");

    public static WslCommand CreateRunInDistroCommand(string distroName, string commandText, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distroName);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);

        return CreateCommandCommand(
            "Run a command inside a WSL distribution",
            distroName,
            commandText,
            timeout);
    }

    private static WslCommand CreateQueryCommand(string description, TimeSpan? timeout, params string[] arguments)
        => new("wsl.exe", arguments, timeout ?? DefaultQueryTimeout, description);

    private static WslCommand CreateMutationCommand(string description, TimeSpan? timeout, params string[] arguments)
        => new("wsl.exe", arguments, timeout ?? DefaultMutationTimeout, description);

    private static WslCommand CreateCommandCommand(string description, string distroName, string commandText, TimeSpan? timeout)
        => new(
            "wsl.exe",
            new[] { "--distribution", distroName, "--exec", "/bin/sh", "-lc", commandText },
            timeout ?? DefaultCommandTimeout,
            description);
}
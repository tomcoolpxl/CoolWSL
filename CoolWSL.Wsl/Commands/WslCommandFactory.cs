using CoolWSL.Core.Models;

namespace CoolWSL.Wsl.Commands;

public static class WslCommandFactory
{
    private static readonly TimeSpan DefaultQueryTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DefaultMutationTimeout = TimeSpan.FromSeconds(30);

    public static WslCommand CreateListVerboseCommand(TimeSpan? timeout = null)
        => CreateQueryCommand("List registered WSL distributions", timeout, "--list", "--verbose");

    public static WslCommand CreateStatusCommand(TimeSpan? timeout = null)
        => CreateQueryCommand("Read WSL status", timeout, "--status");

    public static WslCommand CreateVersionCommand(TimeSpan? timeout = null)
        => CreateQueryCommand("Read WSL version details", timeout, "--version");

    public static WslCommand CreateOpenDistroCommand(string distroName, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distroName);

        return CreateMutationCommand("Open a WSL distribution", timeout, "--distribution", distroName);
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

    private static WslCommand CreateQueryCommand(string description, TimeSpan? timeout, params string[] arguments)
        => new("wsl.exe", arguments, timeout ?? DefaultQueryTimeout, description);

    private static WslCommand CreateMutationCommand(string description, TimeSpan? timeout, params string[] arguments)
        => new("wsl.exe", arguments, timeout ?? DefaultMutationTimeout, description);
}
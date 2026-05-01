using CoolWSL.Core.Models;
using System.ComponentModel;
using System.Globalization;

namespace CoolWSL.Wsl.Errors;

public sealed class WslErrorMapper
{
    public WslCommandError CreateCancellationError()
        => new(
            WslErrorKind.Cancelled,
            "The WSL command was cancelled.",
            "Retry the command when you are ready to let it complete.");

    public WslCommandError CreateTimeoutError(TimeSpan timeout)
        => new(
            WslErrorKind.Timeout,
            FormattableString.Invariant($"The WSL command timed out after {timeout.TotalSeconds:0.#} seconds."),
            "Retry the command with a longer timeout if the operation is expected to take longer.");

    public WslCommandError MapFailure(
        WslCommand command,
        string standardOutput,
        string standardError,
        int? exitCode,
        Exception? launchException = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (launchException is Win32Exception win32Exception && win32Exception.NativeErrorCode == 2)
        {
            return new(
                WslErrorKind.NotInstalled,
                "WSL is not installed or wsl.exe is not available.",
                "Install or repair WSL, then retry the command.");
        }

        if (launchException is not null)
        {
            return new(
                WslErrorKind.LaunchFailed,
                "CoolWSL could not start the requested WSL command.",
                "Verify that WSL is installed and try again.");
        }

        var combinedOutput = string.Join(
            Environment.NewLine,
            new[] { standardError, standardOutput }.Where(static value => !string.IsNullOrWhiteSpace(value)))
            .ToLowerInvariant();

        if (combinedOutput.Contains("invalid command line option", StringComparison.Ordinal) ||
            combinedOutput.Contains("unrecognized option", StringComparison.Ordinal))
        {
            return new(
                WslErrorKind.UnsupportedFeature,
                "This WSL installation does not support that command.",
                "Update WSL to a newer Microsoft Store release and try again.");
        }

        if (combinedOutput.Contains("there is no distribution with the supplied name", StringComparison.Ordinal) ||
            combinedOutput.Contains("distribution was not found", StringComparison.Ordinal))
        {
            return new(
                WslErrorKind.DistroNotFound,
                "The selected Linux distribution was not found.",
                "Refresh the distro list and verify the distribution name before retrying.");
        }

        if (combinedOutput.Contains("access is denied", StringComparison.Ordinal) ||
            combinedOutput.Contains("permission denied", StringComparison.Ordinal))
        {
            return new(
                WslErrorKind.AccessDenied,
                "WSL denied access to the requested operation.",
                "Retry the command with a supported target or adjust permissions manually outside the app.");
        }

        if (combinedOutput.Contains("already running", StringComparison.Ordinal))
        {
            return new(
                WslErrorKind.AlreadyRunning,
                "The selected Linux distribution is already running.",
                "Refresh the distro state and retry only if another action is still needed.");
        }

        if (combinedOutput.Contains("is not running", StringComparison.Ordinal))
        {
            return new(
                WslErrorKind.AlreadyStopped,
                "The selected Linux distribution is already stopped.",
                "Refresh the distro state before retrying the action.");
        }

        if (combinedOutput.Contains("has not been enabled", StringComparison.Ordinal) ||
            combinedOutput.Contains("optional component is not enabled", StringComparison.Ordinal) ||
            combinedOutput.Contains("not supported on this machine", StringComparison.Ordinal))
        {
            return new(
                WslErrorKind.Unavailable,
                "WSL is installed but not currently available on this machine.",
                "Enable the required Windows features or repair the WSL installation, then retry.");
        }

        var detail = exitCode is int code
            ? FormattableString.Invariant($"Review the command output and exit code {code.ToString(CultureInfo.InvariantCulture)} for more detail.")
            : "Review the command output for more detail.";

        return new(WslErrorKind.Unknown, "The WSL command failed.", detail);
    }
}
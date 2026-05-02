using CoolWSL.Core.Helpers;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Models;

namespace CoolWSL.Diagnostics.Mappers;

public sealed class DiagnosticSummaryMapper
{
    public DiagnosticResult CreateStatusResult(CommandResult statusCommand, WslEnvironmentStatus environmentStatus)
    {
        ArgumentNullException.ThrowIfNull(statusCommand);
        ArgumentNullException.ThrowIfNull(environmentStatus);

        return new(
            "wsl-status",
            "WSL status",
            GetSeverity(statusCommand, environmentStatus.IsDegraded),
            environmentStatus.Summary,
            StringHelpers.FirstNonEmpty(environmentStatus.DegradedReason, environmentStatus.SuggestedNextStep, "WSL status loaded."),
            BuildRawOutput(statusCommand),
            environmentStatus.SuggestedNextStep,
            statusCommand.Command.CommandText);
    }

    public DiagnosticResult CreateVersionResult(CommandResult versionCommand, WslEnvironmentStatus environmentStatus)
    {
        ArgumentNullException.ThrowIfNull(versionCommand);
        ArgumentNullException.ThrowIfNull(environmentStatus);

        var summary = versionCommand.IsSuccess
            ? StringHelpers.FirstNonEmpty(
                StringHelpers.JoinNonEmpty("; ",
                    environmentStatus.WslVersion is null ? null : $"WSL {environmentStatus.WslVersion}",
                    environmentStatus.KernelVersion is null ? null : $"Kernel {environmentStatus.KernelVersion}",
                    environmentStatus.WindowsVersion is null ? null : $"Windows {environmentStatus.WindowsVersion}"),
                "WSL version details loaded.")
            : versionCommand.Error?.Summary ?? "WSL version details are unavailable.";

        return new(
            "wsl-version",
            "WSL version",
            GetSeverity(versionCommand, environmentStatus.IsDegraded),
            summary,
            StringHelpers.FirstNonEmpty(versionCommand.Error?.SuggestedNextStep, environmentStatus.DegradedReason, "Review the raw output for version detail."),
            BuildRawOutput(versionCommand),
            versionCommand.Error?.SuggestedNextStep ?? environmentStatus.SuggestedNextStep,
            versionCommand.Command.CommandText);
    }

    public DiagnosticResult CreateInventoryResult(CommandResult inventoryCommand, WslDistroInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventoryCommand);
        ArgumentNullException.ThrowIfNull(inventory);

        var summary = inventory.Distros.Count switch
        {
            > 0 => $"{inventory.Distros.Count} distro{(inventory.Distros.Count == 1 ? string.Empty : "s")} reported.",
            _ => inventory.Summary,
        };

        return new(
            "wsl-inventory",
            "Distro inventory",
            GetSeverity(inventoryCommand, inventory.IsDegraded || inventory.Availability != WslAvailability.Available),
            summary,
            StringHelpers.FirstNonEmpty(inventory.DegradedReason, inventory.SuggestedNextStep, inventory.Summary),
            BuildRawOutput(inventoryCommand),
            inventory.SuggestedNextStep,
            inventoryCommand.Command.CommandText);
    }

    public DiagnosticResult CreateDefaultDistroResult(WslEnvironmentStatus environmentStatus, WslDistroInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(environmentStatus);
        ArgumentNullException.ThrowIfNull(inventory);

        if (!string.IsNullOrWhiteSpace(environmentStatus.DefaultDistroName))
        {
            return new(
                "default-distro",
                "Default distro",
                DiagnosticSeverity.Success,
                $"{environmentStatus.DefaultDistroName} is the current default distro.",
                StringHelpers.FirstNonEmpty(environmentStatus.Summary, "The default distro was reported by WSL."),
                string.Empty);
        }

        if (inventory.Distros.Count == 0)
        {
            return new(
                "default-distro",
                "Default distro",
                DiagnosticSeverity.Info,
                "No distro is installed, so there is no default target.",
                inventory.Summary,
                string.Empty,
                inventory.SuggestedNextStep);
        }

        return new(
            "default-distro",
            "Default distro",
            DiagnosticSeverity.Warning,
            "WSL did not report a default distro.",
            StringHelpers.FirstNonEmpty(environmentStatus.DegradedReason, "Refresh the inventory or use Set default from the distro page if needed."),
            string.Empty,
            environmentStatus.SuggestedNextStep);
    }

    public DiagnosticResult CreateDnsResult(string distroName, CommandResult result)
    {
        ArgumentNullException.ThrowIfNull(distroName);
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return new(
                "dns-resolution",
                "DNS resolution",
                DiagnosticSeverity.Success,
                $"{distroName} resolved learn.microsoft.com successfully.",
                "A DNS lookup tool inside the selected distro returned a result.",
                BuildRawOutput(result),
                null,
                result.Command.CommandText);
        }

        if (ContainsUnsupportedToolMessage(result))
        {
            return new(
                "dns-resolution",
                "DNS resolution",
                DiagnosticSeverity.Warning,
                $"{distroName} does not expose a supported DNS test tool.",
                "Install getent, nslookup, or ping inside the distro to run the DNS check from CoolWSL.",
                BuildRawOutput(result),
                null,
                result.Command.CommandText);
        }

        return new(
            "dns-resolution",
            "DNS resolution",
            DiagnosticSeverity.Error,
            result.Error?.Summary ?? $"{distroName} could not complete the DNS resolution check.",
            StringHelpers.FirstNonEmpty(result.Error?.SuggestedNextStep, "Review the raw output for the failing lookup tool."),
            BuildRawOutput(result),
            result.Error?.SuggestedNextStep,
            result.Command.CommandText);
    }

    public DiagnosticResult CreateInternetResult(string distroName, CommandResult result)
    {
        ArgumentNullException.ThrowIfNull(distroName);
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return new(
                "internet-connectivity",
                "Internet connectivity",
                DiagnosticSeverity.Success,
                $"{distroName} completed the internet connectivity check.",
                "The selected distro reached an external endpoint or IP during the probe.",
                BuildRawOutput(result),
                null,
                result.Command.CommandText);
        }

        if (ContainsUnsupportedToolMessage(result))
        {
            return new(
                "internet-connectivity",
                "Internet connectivity",
                DiagnosticSeverity.Warning,
                $"{distroName} does not expose a supported internet test tool.",
                "Install curl, wget, or ping inside the distro to run the internet probe from CoolWSL.",
                BuildRawOutput(result),
                null,
                result.Command.CommandText);
        }

        return new(
            "internet-connectivity",
            "Internet connectivity",
            DiagnosticSeverity.Error,
            result.Error?.Summary ?? $"{distroName} could not complete the internet connectivity check.",
            StringHelpers.FirstNonEmpty(result.Error?.SuggestedNextStep, "Review the raw output for the failing probe."),
            BuildRawOutput(result),
            result.Error?.SuggestedNextStep,
            result.Command.CommandText);
    }

    public DiagnosticResult CreateHostNote(string? distroName, WslDistro? selectedDistro)
    {
        if (string.IsNullOrWhiteSpace(distroName) || selectedDistro is null)
        {
            return new(
                "host-note",
                "Host-to-WSL notes",
                DiagnosticSeverity.Info,
                "Select a distro to load host-to-WSL notes.",
                "Host networking notes depend on the selected distro and WSL version.",
                string.Empty);
        }

        var summary = selectedDistro.WslVersion switch
        {
            1 => $"{distroName} is running on WSL1, which shares the Windows network stack more directly.",
            2 => $"{distroName} is running on WSL2, which typically exposes Linux services on Windows localhost while keeping its own VM IP.",
            _ => $"{distroName} reported limited WSL version detail, so networking expectations should be treated conservatively.",
        };

        var details = selectedDistro.WslVersion switch
        {
            1 => "WSL1 usually behaves like the host for localhost traffic, but distro-specific firewall or service state can still block access.",
            2 => "WSL2 can still fail DNS or internet checks independently of Windows because the distro uses a virtualized network path.",
            _ => "Refresh the distro inventory after updating WSL if you need more precise networking guidance.",
        };

        return new(
            "host-note",
            "Host-to-WSL notes",
            DiagnosticSeverity.Info,
            summary,
            details,
            string.Empty);
    }

    private static DiagnosticSeverity GetSeverity(CommandResult result, bool isDegraded)
    {
        if (result.IsSuccess)
        {
            return isDegraded ? DiagnosticSeverity.Warning : DiagnosticSeverity.Success;
        }

        return result.Error?.Kind == WslErrorKind.UnsupportedFeature
            ? DiagnosticSeverity.Warning
            : DiagnosticSeverity.Error;
    }

    private static bool ContainsUnsupportedToolMessage(CommandResult result)
    {
        var rawOutput = BuildRawOutput(result);
        return rawOutput.Contains("No supported", StringComparison.OrdinalIgnoreCase) &&
               rawOutput.Contains("tool", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildRawOutput(CommandResult result)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            parts.Add($"stdout{Environment.NewLine}{result.StandardOutput.Trim()}" );
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            parts.Add($"stderr{Environment.NewLine}{result.StandardError.Trim()}" );
        }

        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

}
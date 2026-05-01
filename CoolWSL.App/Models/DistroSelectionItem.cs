using CoolWSL.Core.Models;

namespace CoolWSL.App.Models;

public sealed record DistroSelectionItem(
    string Name,
    string StateLabel,
    string WslVersionLabel,
    string DefaultLabel,
    string ManagementLabel,
    string CapabilityMessage,
    bool IsDefault,
    bool IsRunning,
    bool IsSystemManaged)
{
    public bool CanOpenTerminal => true;

    public bool CanRunCommand => true;

    public bool CanStart => !IsRunning;

    public bool CanTerminate => IsRunning && !IsSystemManaged;

    public bool CanSetDefault => !IsDefault && !IsSystemManaged;

    public bool HasManagementLabel => !string.IsNullOrWhiteSpace(ManagementLabel);

    public static DistroSelectionItem Create(WslDistro distro)
    {
        ArgumentNullException.ThrowIfNull(distro);

        return new(
            distro.Name,
            distro.StateLabel,
            distro.WslVersion?.ToString() ?? "Not reported",
            distro.IsDefault ? "Default distro" : "Not default",
            distro.IsSystemManaged ? "System-managed" : string.Empty,
            BuildCapabilityMessage(distro),
            distro.IsDefault,
            distro.IsRunning,
            distro.IsSystemManaged);
    }

    private static string BuildCapabilityMessage(WslDistro distro)
    {
        if (distro.IsSystemManaged)
        {
            return "System-managed distros stay visible, but terminate and set-default actions stay disabled by default.";
        }

        if (distro.IsRunning)
        {
            return "The distro is running and ready for lifecycle actions, commands, and diagnostics.";
        }

        return "The distro is stopped. Start it or open a terminal before expecting in-distro activity.";
    }
}
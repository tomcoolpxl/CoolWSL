using CoolWSL.Core.Models;

namespace CoolWSL.Core.Helpers;

public static class DistroCapabilityHelper
{
    public static string BuildCapabilityMessage(WslDistro distro)
    {
        ArgumentNullException.ThrowIfNull(distro);

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

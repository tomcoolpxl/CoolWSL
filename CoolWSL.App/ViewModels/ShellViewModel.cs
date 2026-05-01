using CoolWSL.Core.Models;

namespace CoolWSL.App.ViewModels;

public sealed class ShellViewModel
{
    public ShellViewModel()
    {
        Sections =
        [
            new NavigationItem(AppSection.Dashboard, "Dashboard", "Global WSL status, distro inventory, and lifecycle actions."),
            new NavigationItem(AppSection.Distros, "Distros", "Per-distro overview, command runner, and targeted diagnostics."),
            new NavigationItem(AppSection.Diagnostics, "Diagnostics", "Global WSL diagnostics with raw evidence and per-distro checks."),
            new NavigationItem(AppSection.Logs, "Logs", "Metadata-only activity history will appear here."),
            new NavigationItem(AppSection.Settings, "Settings", "Global app preferences will appear here."),
        ];
    }

    public string Title => "CoolWSL";

    public IReadOnlyList<NavigationItem> Sections { get; }
}
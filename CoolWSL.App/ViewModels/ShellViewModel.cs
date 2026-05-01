using CoolWSL.Core.Models;

namespace CoolWSL.App.ViewModels;

public sealed class ShellViewModel
{
    public ShellViewModel()
    {
        Sections =
        [
            new NavigationItem(AppSection.Dashboard, "Dashboard", "Global WSL status and distro inventory."),
            new NavigationItem(AppSection.Distros, "Distros", "Per-distro management will land in later phases."),
            new NavigationItem(AppSection.Diagnostics, "Diagnostics", "Global and per-distro diagnostics will appear here."),
            new NavigationItem(AppSection.Logs, "Logs", "Metadata-only activity history will appear here."),
            new NavigationItem(AppSection.Settings, "Settings", "Global app preferences will appear here."),
        ];
    }

    public string Title => "CoolWSL";

    public IReadOnlyList<NavigationItem> Sections { get; }
}
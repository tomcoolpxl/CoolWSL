using CoolWSL.Core.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CoolWSL.App.Views;

public sealed partial class PlaceholderPage : Page
{
    public PlaceholderPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var section = e.Parameter as AppSection? ?? AppSection.Dashboard;
        SectionTitleBlock.Text = section.ToString();
        SectionDescriptionBlock.Text = section switch
        {
            AppSection.Dashboard => "The dashboard inventory slice lands in Phase 4. The shell is already reserving the navigation host and layout surface.",
            AppSection.Distros => "Per-distro lifecycle and command surfaces will build on this placeholder once the WSL service layer exists.",
            AppSection.Diagnostics => "Diagnostics are intentionally deferred until the safe execution and parsing foundation exists.",
            AppSection.Logs => "Metadata-only logging is baseline behavior; the actual log viewer arrives in a later phase.",
            AppSection.Settings => "Structured and raw settings editors will arrive after the shared shell and service boundaries settle.",
            _ => "This section is reserved for a later phase.",
        };
    }
}
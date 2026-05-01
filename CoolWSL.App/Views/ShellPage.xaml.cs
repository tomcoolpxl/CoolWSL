using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CoolWSL.App.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public ShellViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (RootNavigationView.SelectedItem is null && RootNavigationView.MenuItems.Count > 0)
        {
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        }
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string sectionName ||
            !Enum.TryParse<AppSection>(sectionName, out var section))
        {
            return;
        }

        if (section == AppSection.Dashboard)
        {
            ContentFrame.Navigate(typeof(DashboardPage));
            return;
        }

        ContentFrame.Navigate(typeof(PlaceholderPage), section);
    }
}
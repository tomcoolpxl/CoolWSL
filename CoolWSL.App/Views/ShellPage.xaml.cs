using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoolWSL.App.Views;

public sealed partial class ShellPage : Page
{
    private readonly IDashboardStatusService statusService;
    private readonly StatusBarViewModel statusBarViewModel;

    public ShellPage(
        ShellViewModel viewModel,
        IDashboardStatusService statusService,
        StatusBarViewModel statusBarViewModel)
    {
        ViewModel = viewModel;
        this.statusService = statusService;
        this.statusBarViewModel = statusBarViewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public ShellViewModel ViewModel { get; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (RootNavigationView.SelectedItem is null && RootNavigationView.MenuItems.Count > 0)
        {
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        }

        await PopulateDistrosAsync();
    }

    private async Task PopulateDistrosAsync()
    {
        try
        {
            var snapshot = await statusService.GetSnapshotAsync(CancellationToken.None);
            statusBarViewModel.ApplySnapshot(snapshot);
            ApplyDistroItems(snapshot.DistroInventory);
        }
        catch (Exception)
        {
            statusBarViewModel.SetUnavailable();
            ApplyDistroItems(new WslDistroInventory(WslAvailability.Unavailable, Array.Empty<WslDistro>(), "Inventory unavailable"));
        }
    }

    private void ApplyDistroItems(WslDistroInventory inventory)
    {
        for (var i = RootNavigationView.MenuItems.Count - 1; i >= 0; i--)
        {
            if (RootNavigationView.MenuItems[i] is NavigationViewItem item && item.Tag is WslDistro)
            {
                RootNavigationView.MenuItems.RemoveAt(i);
            }
        }

        var headerIndex = RootNavigationView.MenuItems.IndexOf(DistrosHeader);
        if (headerIndex < 0)
        {
            return;
        }

        var insertAt = headerIndex + 1;

        if (inventory.Distros.Count == 0)
        {
            DistrosLoadingItem.Content = "No distros installed";
            DistrosLoadingItem.Visibility = Visibility.Visible;
            return;
        }

        DistrosLoadingItem.Visibility = Visibility.Collapsed;

        foreach (var distro in inventory.Distros)
        {
            var item = new NavigationViewItem
            {
                Content = distro.Name,
                Tag = distro,
                Icon = new FontIcon { Glyph = "" },
            };
            RootNavigationView.MenuItems.Insert(insertAt++, item);
        }
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is WslDistro distro)
        {
            ContentFrame.Navigate(typeof(DistroPage), distro.Name);
            return;
        }

        if (args.SelectedItemContainer?.Tag is not string sectionName)
        {
            return;
        }

        switch (sectionName)
        {
            case "Dashboard":
                ContentFrame.Navigate(typeof(DashboardPage));
                break;
            case "Diagnostics":
                ContentFrame.Navigate(typeof(DiagnosticsPage));
                break;
            case "Settings":
                ContentFrame.Navigate(typeof(PlaceholderPage), AppSection.Settings);
                break;
        }
    }
}

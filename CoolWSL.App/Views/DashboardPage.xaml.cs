using CoolWSL.App.Dialogs.ConfirmationDialogs;
using CoolWSL.App.Models;
using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CoolWSL.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
        : this(App.Services.GetRequiredService<DashboardViewModel>())
    {
    }

    internal DashboardPage(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public DashboardViewModel ViewModel { get; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.EnsureLoadedAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void OnOpenDefaultClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenDefaultDistroAsync();
    }

    private async void OnShutdownAllClick(object sender, RoutedEventArgs e)
    {
        var request = new OperationRequest(
            "Shutdown all WSL instances?",
            "Shutdown All",
            "Every running WSL distro and the shared WSL 2 utility VM.",
            "This immediately stops all running distros, not just the currently selected one.");

        if (!await ConfirmAsync(request))
        {
            return;
        }

        await ViewModel.ShutdownAsync();
    }

    private void OnDistroTileClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DashboardDistroRow row)
        {
            return;
        }

        var navigationView = FindParentNavigationView();
        if (navigationView is not null)
        {
            foreach (var item in navigationView.MenuItems)
            {
                if (item is NavigationViewItem nvi &&
                    nvi.Tag is WslDistro distro &&
                    string.Equals(distro.Name, row.Name, StringComparison.Ordinal))
                {
                    navigationView.SelectedItem = nvi;
                    return;
                }
            }
        }

        Frame?.Navigate(typeof(DistroPage), row.Name);
    }

    private NavigationView? FindParentNavigationView()
    {
        DependencyObject? parent = this;
        while (parent is not null)
        {
            parent = VisualTreeHelper.GetParent(parent);
            if (parent is NavigationView nav)
            {
                return nav;
            }
        }
        return null;
    }

    private async Task<bool> ConfirmAsync(OperationRequest request)
    {
        var dialog = new OperationConfirmationDialog(request)
        {
            XamlRoot = XamlRoot,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}

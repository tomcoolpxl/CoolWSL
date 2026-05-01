using CoolWSL.App.Dialogs.ConfirmationDialogs;
using CoolWSL.App.Models;
using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    private async void OnOpenDistroClick(object sender, RoutedEventArgs e)
    {
        if (GetRow(sender) is not { } row)
        {
            return;
        }

        await ViewModel.OpenDistroAsync(row.Name);
    }

    private async void OnStartDistroClick(object sender, RoutedEventArgs e)
    {
        if (GetRow(sender) is not { } row)
        {
            return;
        }

        await ViewModel.StartDistroAsync(row.Name);
    }

    private async void OnTerminateDistroClick(object sender, RoutedEventArgs e)
    {
        if (GetRow(sender) is not { } row)
        {
            return;
        }

        var request = new OperationRequest(
            $"Terminate {row.Name}?",
            "Terminate",
            row.Name,
            "This immediately stops the selected distro and any running processes inside it.",
            "Cancel is the safe default if you are not sure that the distro can be stopped right now.");

        if (!await ConfirmAsync(request))
        {
            return;
        }

        await ViewModel.TerminateDistroAsync(row.Name);
    }

    private async void OnSetDefaultClick(object sender, RoutedEventArgs e)
    {
        if (GetRow(sender) is not { } row)
        {
            return;
        }

        await ViewModel.SetDefaultDistroAsync(row.Name);
    }

    private async Task<bool> ConfirmAsync(OperationRequest request)
    {
        var dialog = new OperationConfirmationDialog(request)
        {
            XamlRoot = XamlRoot,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private static DashboardDistroRow? GetRow(object sender)
        => (sender as FrameworkElement)?.DataContext as DashboardDistroRow;
}
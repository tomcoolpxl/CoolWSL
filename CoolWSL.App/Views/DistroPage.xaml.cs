using CoolWSL.App.Dialogs.ConfirmationDialogs;
using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace CoolWSL.App.Views;

public sealed partial class DistroPage : Page
{
    public DistroPage()
        : this(App.Services.GetRequiredService<DistroViewModel>())
    {
    }

    internal DistroPage(DistroViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public DistroViewModel ViewModel { get; }

    private Visibility GetVisibleWhen(bool value)
        => value ? Visibility.Visible : Visibility.Collapsed;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.EnsureLoadedAsync(e.Parameter as string);
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void OnOpenTerminalClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenTerminalAsync();
    }

    private async void OnStartClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartDistroAsync();
    }

    private async void OnTerminateClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.SelectedDistroName))
        {
            return;
        }

        var request = new OperationRequest(
            $"Terminate {ViewModel.SelectedDistroName}?",
            "Terminate",
            ViewModel.SelectedDistroName,
            "This immediately stops the selected distro and any running processes inside it.",
            "Cancel is the safe default if you are not sure that the distro can be stopped right now.");

        if (!await ConfirmAsync(request))
        {
            return;
        }

        await ViewModel.TerminateDistroAsync();
    }

    private async void OnSetDefaultClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.SetDefaultDistroAsync();
    }

    private async void OnReloadSettingsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.Settings.LoadAsync();
    }

    private async void OnVerifySettingsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.Settings.VerifyAsync();
    }

    private async void OnSaveSettingsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.Settings.SaveAsync();
    }

    private async void OnSaveAndTerminateClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.Settings.SaveAsync();
        if (ViewModel.Settings.Errors.Count == 0)
        {
            await ViewModel.TerminateDistroAsync();
        }
    }

    private async void OnRevertSettingsClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.Settings.IsModified)
        {
            return;
        }

        var distroLabel = string.IsNullOrWhiteSpace(ViewModel.SelectedDistroName)
            ? "selected distro"
            : ViewModel.SelectedDistroName;

        var request = new OperationRequest(
            "Revert unsaved changes?",
            "Revert",
            distroLabel,
            "This discards every edit since the last load of /etc/wsl.conf.",
            "Saved files on the distro are not modified.");

        if (!await ConfirmAsync(request))
        {
            return;
        }

        ViewModel.Settings.Revert();
    }

    private async void OnRestoreDefaultsClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.SelectedDistroName))
        {
            return;
        }

        var request = new OperationRequest(
            $"Restore defaults for {ViewModel.SelectedDistroName}?",
            "Restore defaults",
            ViewModel.SelectedDistroName,
            "This deletes /etc/wsl.conf inside the distro so WSL falls back to its built-in defaults.",
            "A timestamped backup of the current file is kept under %LocalAppData%\\CoolWSL\\Backups\\WslDistroConfig before the file is removed.");

        if (!await ConfirmAsync(request))
        {
            return;
        }

        await ViewModel.Settings.RestoreDefaultsAsync();
    }

    private void OnOpenWslSettingsClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Settings.OpenWslSettings();
    }

    private async void OnRefreshDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.Diagnostics.RefreshAsync();
    }

    private static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
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

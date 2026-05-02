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

    private async void OnRunCommandClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CommandRunner.RunAsync();
    }

    private void OnCancelCommandClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CommandRunner.Cancel();
    }

    private void OnCopyStdoutClick(object sender, RoutedEventArgs e)
    {
        CopyToClipboard(ViewModel.CommandRunner.StandardOutput);
    }

    private void OnCopyStderrClick(object sender, RoutedEventArgs e)
    {
        CopyToClipboard(ViewModel.CommandRunner.StandardError);
    }

    private void OnClearOutputClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CommandRunner.ClearOutput();
    }

    private void OnReuseHistoryEntryClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CommandHistoryEntry entry)
        {
            ViewModel.CommandRunner.ReuseHistoryEntry(entry);
            CommandInputBox.StartBringIntoView();
            CommandInputBox.Focus(FocusState.Programmatic);
        }
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

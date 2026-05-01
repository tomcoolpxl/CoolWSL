using CoolWSL.App.Dialogs.ConfirmationDialogs;
using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

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

    private async void OnDistroSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: CoolWSL.App.Models.DistroSelectionItem selectedItem })
        {
            return;
        }

        await ViewModel.SelectDistroAsync(selectedItem.Name);
    }

    private async void OnOpenTerminalClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenTerminalAsync();
    }

    private void OnFocusCommandRunnerClick(object sender, RoutedEventArgs e)
    {
        CommandRunnerSection.StartBringIntoView();
        CommandInputBox.Focus(FocusState.Programmatic);
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

    private void OnReuseHistoryEntryClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is CommandHistoryEntry entry)
        {
            ViewModel.CommandRunner.ReuseHistoryEntry(entry);
            CommandRunnerSection.StartBringIntoView();
            CommandInputBox.Focus(FocusState.Programmatic);
        }
    }

    private async void OnRefreshDiagnosticsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshDiagnosticsAsync();
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
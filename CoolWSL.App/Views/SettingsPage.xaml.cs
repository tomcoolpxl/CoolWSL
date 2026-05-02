using System.Reflection;
using CoolWSL.App.Dialogs.ConfirmationDialogs;
using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace CoolWSL.App.Views;

public sealed partial class SettingsPage : Page
{
    private const string RepositoryUrl = "https://github.com/tomcoolpxl/CoolWSL";
    private const string IssueUrl = "https://github.com/tomcoolpxl/CoolWSL/issues";

    public SettingsPage()
        : this(App.Services.GetRequiredService<SettingsViewModel>())
    {
    }

    internal SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        AppVersion = GetAppVersion();
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public SettingsViewModel ViewModel { get; }

    public string AppVersion { get; }

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

        await ViewModel.ShutdownAllAsync();
    }

    private void OnOpenWslSettingsClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenWslSettings();
    }

    private async void OnOpenRepositoryClick(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri(RepositoryUrl));
    }

    private async void OnReportIssueClick(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri(IssueUrl));
    }

    private async Task<bool> ConfirmAsync(OperationRequest request)
    {
        var dialog = new OperationConfirmationDialog(request)
        {
            XamlRoot = XamlRoot,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private static string GetAppVersion()
    {
        var assembly = typeof(SettingsPage).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString() ?? "Development build"
            : informationalVersion;
    }
}

using CoolWSL.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoolWSL.App.Views;

public sealed partial class LogsPage : Page
{
    public LogsPage()
        : this(App.Services.GetRequiredService<LogsViewModel>())
    {
    }

    internal LogsPage(LogsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public LogsViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshLogs();
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        RefreshLogs();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        var levelFilter = (LevelFilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        ViewModel.Clear(levelFilter, SearchBox.Text);
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        RefreshLogs();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        RefreshLogs();
    }

    private void RefreshLogs()
    {
        var levelFilter = (LevelFilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        ViewModel.Refresh(levelFilter, SearchBox.Text);
    }
}

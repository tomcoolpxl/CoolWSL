using CoolWSL.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoolWSL.App.Views;

public sealed partial class DiagnosticsPage : Page
{
    public DiagnosticsPage()
        : this(App.Services.GetRequiredService<DiagnosticsViewModel>())
    {
    }

    internal DiagnosticsPage(DiagnosticsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public DiagnosticsViewModel ViewModel { get; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.EnsureLoadedAsync();
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

    private async void OnRetryClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync(ViewModel.SelectedDistroName);
    }
}
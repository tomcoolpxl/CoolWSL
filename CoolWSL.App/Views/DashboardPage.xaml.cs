using CoolWSL.App.ViewModels;
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
}
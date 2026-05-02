using CoolWSL.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CoolWSL.App.Views;

public sealed partial class StatusBar : UserControl
{
    public StatusBar(StatusBarViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public StatusBarViewModel ViewModel { get; }
}

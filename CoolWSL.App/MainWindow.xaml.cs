using CoolWSL.App.Views;
using Microsoft.UI.Xaml;

namespace CoolWSL.App;

public sealed partial class MainWindow : Window
{
    public MainWindow(ShellPage shellPage)
    {
        InitializeComponent();

        Title = "CoolWSL";
        RootGrid.Children.Add(shellPage);
    }
}
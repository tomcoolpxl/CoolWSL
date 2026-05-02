using CoolWSL.App.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CoolWSL.App;

public sealed partial class MainWindow : Window
{
    public MainWindow(ShellPage shellPage, StatusBar statusBar)
    {
        InitializeComponent();

        Title = "CoolWSL";
        RootGrid.Children.Add(shellPage);
        StatusBarHost.Children.Add(statusBar);

        AppTitleBar.Loaded += OnAppTitleBarLoaded;
        AppTitleBar.SizeChanged += OnAppTitleBarSizeChanged;

        ConfigureTitleBar();
    }

    private void ConfigureTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var titleBar = AppWindow.TitleBar;
        titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        if (Application.Current.Resources["SubtleFillColorSecondaryBrush"] is SolidColorBrush hoverBrush)
        {
            titleBar.ButtonHoverBackgroundColor = hoverBrush.Color;
        }

        if (Application.Current.Resources["SubtleFillColorTertiaryBrush"] is SolidColorBrush pressedBrush)
        {
            titleBar.ButtonPressedBackgroundColor = pressedBrush.Color;
        }

        UpdateTitleBarInsets();
    }

    private void OnAppTitleBarLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTitleBarInsets();
    }

    private void OnAppTitleBarSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTitleBarInsets();
    }

    private void UpdateTitleBarInsets()
    {
        if (!ExtendsContentIntoTitleBar || AppTitleBar.XamlRoot is null || !AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var scale = AppTitleBar.XamlRoot.RasterizationScale;
        LeftInsetColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scale);
        RightInsetColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scale);
    }
}

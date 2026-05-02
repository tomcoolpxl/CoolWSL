using CoolWSL.App.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CoolWSL.App;

public sealed partial class MainWindow : Window
{
    public MainWindow(ShellPage shellPage)
    {
        InitializeComponent();

        Title = "CoolWSL";
        RootGrid.Children.Add(shellPage);

        Activated += OnActivated;
        AppTitleBar.Loaded += OnAppTitleBarLoaded;
        AppTitleBar.SizeChanged += OnAppTitleBarSizeChanged;

        ConfigureTitleBar();
    }

    private void ConfigureTitleBar()
    {
        TitleBarTitleTextBlock.Text = Title;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var titleBar = AppWindow.TitleBar;
        titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(20, 255, 255, 255);
        titleBar.ButtonPressedBackgroundColor = Color.FromArgb(36, 255, 255, 255);

        UpdateTitleBarInsets();
        UpdateTitleBarActivationState(WindowActivationState.CodeActivated);
    }

    private void OnAppTitleBarLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTitleBarInsets();
    }

    private void OnAppTitleBarSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTitleBarInsets();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        UpdateTitleBarActivationState(args.WindowActivationState);
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

    private void UpdateTitleBarActivationState(WindowActivationState activationState)
    {
        var isActive = activationState != WindowActivationState.Deactivated;
        TitleBarTitleTextBlock.Opacity = isActive ? 1 : 0.68;
        TitleBarSubtitleTextBlock.Opacity = isActive ? 0.72 : 0.48;

        if (AppTitleBar.Background is SolidColorBrush backgroundBrush)
        {
            backgroundBrush.Opacity = isActive ? 1 : 0.88;
        }
    }
}
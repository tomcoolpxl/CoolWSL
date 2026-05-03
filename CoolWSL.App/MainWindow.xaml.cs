using CoolWSL.App.Services;
using CoolWSL.App.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.IO;

namespace CoolWSL.App;

public sealed partial class MainWindow : Window
{
    private readonly IThemePreferenceService themePreferenceService;

    public MainWindow(ShellPage shellPage, StatusBar statusBar, IThemePreferenceService themePreferenceService)
    {
        this.themePreferenceService = themePreferenceService;
        InitializeComponent();

        Title = "CoolWSL";
        RootGrid.Children.Add(shellPage);
        StatusBarHost.Children.Add(statusBar);

        AppTitleBar.Loaded += OnAppTitleBarLoaded;
        AppTitleBar.SizeChanged += OnAppTitleBarSizeChanged;
        Activated += OnWindowActivated;
        themePreferenceService.ThemeChanged += OnThemeChanged;

        if (Content is FrameworkElement rootElement)
        {
            rootElement.ActualThemeChanged += OnRootActualThemeChanged;
        }

        ApplyTheme(themePreferenceService.CurrentTheme);
        ConfigureWindowIcon();
        ConfigureTitleBar();
    }

    private void ConfigureWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");

        if (!File.Exists(iconPath))
        {
            return;
        }

        AppWindow.SetIcon(iconPath);
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
        UpdateTitleBarButtonColors();
        UpdateTitleBarInsets();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyTheme(themePreferenceService.CurrentTheme);
    }

    private void OnRootActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateTitleBarButtonColors();
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
    {
        UpdateTitleBarButtonColors();
    }

    private void ApplyTheme(AppThemePreference themePreference)
    {
        if (Content is not FrameworkElement rootElement)
        {
            return;
        }

        rootElement.RequestedTheme = themePreference switch
        {
            AppThemePreference.Light => ElementTheme.Light,
            AppThemePreference.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        UpdateTitleBarButtonColors();
    }

    private void UpdateTitleBarButtonColors()
    {
        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var activeTheme = (Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default;
        var isDark = activeTheme == ElementTheme.Dark;
        var titleBar = AppWindow.TitleBar;

        titleBar.ButtonForegroundColor = isDark
            ? ColorHelper.FromArgb(0xFF, 0xF5, 0xF5, 0xF5)
            : ColorHelper.FromArgb(0xFF, 0x1C, 0x1C, 0x1C);
        titleBar.ButtonInactiveForegroundColor = isDark
            ? ColorHelper.FromArgb(0xCC, 0xF5, 0xF5, 0xF5)
            : ColorHelper.FromArgb(0xCC, 0x1C, 0x1C, 0x1C);
        titleBar.ButtonHoverForegroundColor = titleBar.ButtonForegroundColor;
        titleBar.ButtonPressedForegroundColor = titleBar.ButtonForegroundColor;
        titleBar.ButtonHoverBackgroundColor = isDark
            ? ColorHelper.FromArgb(0x24, 0xFF, 0xFF, 0xFF)
            : ColorHelper.FromArgb(0x14, 0x00, 0x00, 0x00);
        titleBar.ButtonPressedBackgroundColor = isDark
            ? ColorHelper.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
            : ColorHelper.FromArgb(0x1F, 0x00, 0x00, 0x00);
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

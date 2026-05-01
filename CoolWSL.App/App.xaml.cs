using CoolWSL.App.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace CoolWSL.App;

public partial class App : Application
{
    private Window? mainWindow;

    public App()
    {
        InitializeComponent();
        Services = AppServiceCollection.Build();
    }

    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        mainWindow ??= Services.GetRequiredService<MainWindow>();
        mainWindow.Activate();
    }
}
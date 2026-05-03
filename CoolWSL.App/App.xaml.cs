using CoolWSL.App.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace CoolWSL.App;

public partial class App : Application
{
    private const string SmokeTestEnabledVariable = "COOLWSL_SMOKE_TEST";
    private const string SmokeTestMarkerPathVariable = "COOLWSL_SMOKE_TEST_FILE";
    private Window? mainWindow;

    public App()
    {
        InitializeComponent();
        Services = AppServiceCollection.Build();
    }

    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (IsSmokeTestEnabled())
        {
            WriteSmokeTestMarker();
            Exit();
            return;
        }

        mainWindow ??= Services.GetRequiredService<MainWindow>();

        mainWindow.Activate();
    }

    private static bool IsSmokeTestEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable(SmokeTestEnabledVariable),
            "1",
            StringComparison.Ordinal);
    }

    private static void WriteSmokeTestMarker()
    {
        var markerPath = Environment.GetEnvironmentVariable(SmokeTestMarkerPathVariable);

        if (string.IsNullOrWhiteSpace(markerPath))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(markerPath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(markerPath, DateTimeOffset.UtcNow.ToString("O"));
    }

}
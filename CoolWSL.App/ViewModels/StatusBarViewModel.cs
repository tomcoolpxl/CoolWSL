using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CoolWSL.App.ViewModels;

public sealed class StatusBarViewModel : INotifyPropertyChanged
{
    private readonly IDashboardStatusService statusService;
    private string wslStatusText = "WSL status unknown";
    private string distroSummary = "Loading distros…";
    private string defaultDistroText = "No default distro";
    private string lastRefreshedText = "Not refreshed yet";
    private Brush? indicatorBrush;

    public StatusBarViewModel(IDashboardStatusService statusService)
    {
        this.statusService = statusService;
        indicatorBrush = ResolveBrush("TextFillColorTertiaryBrush");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WslStatusText
    {
        get => wslStatusText;
        private set => Set(ref wslStatusText, value);
    }

    public string DistroSummary
    {
        get => distroSummary;
        private set => Set(ref distroSummary, value);
    }

    public string DefaultDistroText
    {
        get => defaultDistroText;
        private set => Set(ref defaultDistroText, value);
    }

    public string LastRefreshedText
    {
        get => lastRefreshedText;
        private set => Set(ref lastRefreshedText, value);
    }

    public Brush? IndicatorBrush
    {
        get => indicatorBrush;
        private set => Set(ref indicatorBrush, value);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await statusService.GetSnapshotAsync(cancellationToken);
            ApplySnapshot(snapshot);
        }
        catch (Exception)
        {
            SetUnavailable();
        }
    }

    public void ApplySnapshot(DashboardStatusSnapshot snapshot)
    {
        var environment = snapshot.EnvironmentStatus;
        var inventory = snapshot.DistroInventory;

        WslStatusText = environment.Availability switch
        {
            WslAvailability.Available => string.IsNullOrWhiteSpace(environment.WslVersion)
                ? "WSL available"
                : $"WSL {environment.WslVersion}",
            WslAvailability.NotInstalled => "WSL not installed",
            _ => "WSL unavailable",
        };

        var totalDistros = inventory.Distros.Count;
        var runningDistros = inventory.Distros.Count(static distro => distro.IsRunning);
        DistroSummary = totalDistros == 0
            ? "No distros installed"
            : $"{totalDistros} distro{(totalDistros == 1 ? string.Empty : "s")} • {runningDistros} running";

        DefaultDistroText = string.IsNullOrWhiteSpace(environment.DefaultDistroName)
            ? "No default distro"
            : $"Default: {environment.DefaultDistroName}";

        LastRefreshedText = $"Refreshed {DateTimeOffset.Now.LocalDateTime:HH:mm:ss}";

        IndicatorBrush = environment.Availability switch
        {
            WslAvailability.Available => ResolveBrush("SystemFillColorSuccessBrush"),
            WslAvailability.NotInstalled => ResolveBrush("SystemFillColorCautionBrush"),
            _ => ResolveBrush("SystemFillColorCriticalBrush"),
        };
    }

    public void SetUnavailable()
    {
        WslStatusText = "WSL status unavailable";
        DistroSummary = "Inventory unavailable";
        DefaultDistroText = "Default unknown";
        LastRefreshedText = $"Failed at {DateTimeOffset.Now.LocalDateTime:HH:mm:ss}";
        IndicatorBrush = ResolveBrush("SystemFillColorCriticalBrush");
    }

    private static Brush? ResolveBrush(string key)
    {
        if (Application.Current?.Resources is null)
        {
            return null;
        }

        return Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush
            ? brush
            : null;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

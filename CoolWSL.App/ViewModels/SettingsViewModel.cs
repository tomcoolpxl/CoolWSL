using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.App.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IDashboardStatusService statusService;
    private readonly IWslDistroService distroService;
    private bool hasLoaded;
    private bool hasDefaultDistro;
    private bool isLoading;
    private bool isActionInProgress;
    private string wslStatusText = "Not loaded.";
    private string defaultDistroText = "Not loaded.";
    private string distroSummaryText = "Not loaded.";
    private string lastLoadedText = "Not refreshed yet.";
    private string actionStatusText = string.Empty;

    public SettingsViewModel(IDashboardStatusService statusService, IWslDistroService distroService)
    {
        this.statusService = statusService;
        this.distroService = distroService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading
    {
        get => isLoading;
        private set
        {
            if (isLoading == value)
            {
                return;
            }

            isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool IsActionInProgress
    {
        get => isActionInProgress;
        private set
        {
            if (isActionInProgress == value)
            {
                return;
            }

            isActionInProgress = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRunActions));
            OnPropertyChanged(nameof(CanOpenDefaultDistro));
        }
    }

    public string WslStatusText
    {
        get => wslStatusText;
        private set
        {
            if (string.Equals(wslStatusText, value, StringComparison.Ordinal))
            {
                return;
            }

            wslStatusText = value;
            OnPropertyChanged();
        }
    }

    public string DefaultDistroText
    {
        get => defaultDistroText;
        private set
        {
            if (string.Equals(defaultDistroText, value, StringComparison.Ordinal))
            {
                return;
            }

            defaultDistroText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanOpenDefaultDistro));
        }
    }

    public string DistroSummaryText
    {
        get => distroSummaryText;
        private set
        {
            if (string.Equals(distroSummaryText, value, StringComparison.Ordinal))
            {
                return;
            }

            distroSummaryText = value;
            OnPropertyChanged();
        }
    }

    public string LastLoadedText
    {
        get => lastLoadedText;
        private set
        {
            if (string.Equals(lastLoadedText, value, StringComparison.Ordinal))
            {
                return;
            }

            lastLoadedText = value;
            OnPropertyChanged();
        }
    }

    public string ActionStatusText
    {
        get => actionStatusText;
        private set
        {
            if (string.Equals(actionStatusText, value, StringComparison.Ordinal))
            {
                return;
            }

            actionStatusText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasActionStatus));
        }
    }

    public bool HasActionStatus => !string.IsNullOrWhiteSpace(ActionStatusText);

    public bool CanRunActions => !IsActionInProgress;

    public bool CanOpenDefaultDistro => CanRunActions && hasDefaultDistro;

    public async Task EnsureLoadedAsync()
    {
        if (hasLoaded || IsLoading)
        {
            return;
        }

        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        IsLoading = true;

        try
        {
            var snapshot = await statusService.GetSnapshotAsync(CancellationToken.None);
            ApplySnapshot(snapshot);
            hasLoaded = true;
        }
        catch (Exception ex)
        {
            hasDefaultDistro = false;
            WslStatusText = "WSL status could not be loaded.";
            DefaultDistroText = "No default distro reported.";
            DistroSummaryText = ex.Message;
            LastLoadedText = $"Refresh failed {DateTimeOffset.Now:t}";
            OnPropertyChanged(nameof(CanOpenDefaultDistro));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public Task OpenDefaultDistroAsync()
    {
        if (!CanOpenDefaultDistro)
        {
            ActionStatusText = "WSL did not report a default distro to open.";
            return Task.CompletedTask;
        }

        return ExecuteActionAsync(
            cancellationToken => distroService.OpenDefaultDistroAsync(cancellationToken),
            "Opened the default distro in a terminal.",
            refreshAfterSuccess: false);
    }

    public Task ShutdownAllAsync()
        => ExecuteActionAsync(
            cancellationToken => distroService.ShutdownAsync(cancellationToken),
            "Shut down all running WSL distros.",
            refreshAfterSuccess: true);

    private void ApplySnapshot(DashboardStatusSnapshot snapshot)
    {
        var status = snapshot.EnvironmentStatus;
        WslStatusText = status switch
        {
            { Availability: WslAvailability.Available, IsDegraded: true } => $"WSL is degraded: {status.Summary}",
            { Availability: WslAvailability.Available } when !string.IsNullOrWhiteSpace(status.WslVersion) => $"WSL {status.WslVersion}",
            { Availability: WslAvailability.Available } => "WSL is available.",
            { Availability: WslAvailability.NotInstalled } => $"WSL is not installed: {status.Summary}",
            { Availability: WslAvailability.Unavailable } => $"WSL is unavailable: {status.Summary}",
            _ => status.Summary,
        };

        hasDefaultDistro = !string.IsNullOrWhiteSpace(status.DefaultDistroName);
        DefaultDistroText = !hasDefaultDistro
            ? "No default distro reported."
            : status.DefaultDistroName!;

        var runningCount = snapshot.DistroInventory.Distros.Count(static distro => distro.IsRunning);
        DistroSummaryText = $"{snapshot.DistroInventory.Distros.Count} distros installed, {runningCount} running.";
        LastLoadedText = $"Refreshed {DateTimeOffset.Now:t}";
        OnPropertyChanged(nameof(CanOpenDefaultDistro));
    }

    private async Task ExecuteActionAsync(
        Func<CancellationToken, Task<CommandResult>> action,
        string successMessage,
        bool refreshAfterSuccess)
    {
        if (IsActionInProgress)
        {
            return;
        }

        IsActionInProgress = true;

        try
        {
            var result = await action(CancellationToken.None);
            ActionStatusText = result.IsSuccess
                ? successMessage
                : result.Error?.Summary ?? "The WSL action failed.";

            if (refreshAfterSuccess && result.IsSuccess)
            {
                await RefreshAsync();
            }
        }
        finally
        {
            IsActionInProgress = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

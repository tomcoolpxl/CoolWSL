using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.App.Models;
using CoolWSL.App.Services;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Helpers;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.App.ViewModels;

public sealed class DistroViewModel : INotifyPropertyChanged
{
    private readonly IDashboardStatusService dashboardStatusService;
    private readonly IWslDistroService distroService;
    private readonly RefreshCoordinator pageRefreshCoordinator = new();
    private string actionStatusText = string.Empty;
    private IReadOnlyList<DistroSelectionItem> distros = Array.Empty<DistroSelectionItem>();
    private string emptyStateMessage = "Refresh the distro page to load the current distro inventory.";
    private string emptyStateTitle = "No distro selected";
    private bool hasLoaded;
    private bool isLoading;
    private DateTimeOffset? lastUpdatedAt;
    private DistroSelectionItem? selectedDistro;
    private string summaryText = "Select a distro to inspect lifecycle actions, commands, and diagnostics.";
    private string warningText = string.Empty;

    public DistroViewModel(
        IDashboardStatusService dashboardStatusService,
        IWslDistroService distroService,
        DistroSettingsViewModel settings,
        DistroPageDiagnosticsViewModel diagnostics)
    {
        this.dashboardStatusService = dashboardStatusService;
        this.distroService = distroService;
        Settings = settings;
        Diagnostics = diagnostics;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DistroSettingsViewModel Settings { get; }

    public DistroPageDiagnosticsViewModel Diagnostics { get; }

    public IReadOnlyList<DistroSelectionItem> Distros
    {
        get => distros;
        private set
        {
            distros = value;
            OnPropertyChanged();
        }
    }

    public DistroSelectionItem? SelectedDistro => selectedDistro;

    public string? SelectedDistroName => selectedDistro?.Name;

    public string HeaderName => selectedDistro?.Name ?? "No distro selected";

    public string HeaderState => selectedDistro?.StateLabel ?? "Unavailable";

    public string HeaderWslVersion => selectedDistro?.WslVersionLabel ?? "Not reported";

    public string HeaderDefault => selectedDistro?.DefaultLabel ?? string.Empty;

    public string HeaderManagementLabel => selectedDistro?.ManagementLabel ?? string.Empty;

    public string HeaderMetadataText
        => string.Join(
            " - ",
            new[] { HeaderWslVersion, HeaderDefault, HeaderManagementLabel }
                .Where(static value => !string.IsNullOrWhiteSpace(value)));

    public string HeaderCapabilityMessage => selectedDistro?.CapabilityMessage ?? emptyStateMessage;

    public bool HasSelection => selectedDistro is not null;

    public bool ShowEmptyState => selectedDistro is null;

    public bool HasDefaultLabel => !string.IsNullOrWhiteSpace(HeaderDefault);

    public bool HasManagementLabel => selectedDistro?.HasManagementLabel == true;

    public bool CanOpenTerminal => selectedDistro?.CanOpenTerminal == true;

    public bool CanRunCommand => selectedDistro?.CanRunCommand == true;

    public bool CanStart => selectedDistro?.CanStart == true;

    public bool CanTerminate => selectedDistro?.CanTerminate == true;

    public bool CanSetDefault => selectedDistro?.CanSetDefault == true;

    public string SummaryText
    {
        get => summaryText;
        private set
        {
            if (string.Equals(summaryText, value, StringComparison.Ordinal))
            {
                return;
            }

            summaryText = value;
            OnPropertyChanged();
        }
    }

    public string EmptyStateTitle
    {
        get => emptyStateTitle;
        private set
        {
            if (string.Equals(emptyStateTitle, value, StringComparison.Ordinal))
            {
                return;
            }

            emptyStateTitle = value;
            OnPropertyChanged();
        }
    }

    public string EmptyStateMessage
    {
        get => emptyStateMessage;
        private set
        {
            if (string.Equals(emptyStateMessage, value, StringComparison.Ordinal))
            {
                return;
            }

            emptyStateMessage = value;
            OnPropertyChanged();
        }
    }

    public string RefreshStatusText
        => lastUpdatedAt.HasValue
            ? $"Last refreshed {lastUpdatedAt.Value.LocalDateTime:g}."
            : "The distro page has not been loaded yet.";

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

    public bool HasLoaded
    {
        get => hasLoaded;
        private set
        {
            if (hasLoaded == value)
            {
                return;
            }

            hasLoaded = value;
            OnPropertyChanged();
        }
    }

    public string WarningText
    {
        get => warningText;
        private set
        {
            if (string.Equals(warningText, value, StringComparison.Ordinal))
            {
                return;
            }

            warningText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasWarning));
        }
    }

    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningText);

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

    public async Task EnsureLoadedAsync(string? preferredDistroName = null)
    {
        if (HasLoaded && string.IsNullOrWhiteSpace(preferredDistroName))
        {
            return;
        }

        await RefreshAsync(preferredDistroName);
    }

    public async Task RefreshAsync(string? preferredDistroName = null)
    {
        var lease = pageRefreshCoordinator.Start();
        IsLoading = true;

        try
        {
            var snapshot = await dashboardStatusService.GetSnapshotAsync(lease.CancellationToken);
            if (!pageRefreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            Distros = snapshot.DistroInventory.Distros.Select(DistroSelectionItem.Create).ToArray();
            SummaryText = BuildSummary(snapshot);
            WarningText = StringHelpers.CombineDistinct(snapshot.EnvironmentStatus.DegradedReason, snapshot.DistroInventory.DegradedReason);
            lastUpdatedAt = DateTimeOffset.Now;
            OnPropertyChanged(nameof(RefreshStatusText));
            HasLoaded = true;
            IsLoading = false;

            if (Distros.Count == 0)
            {
                EmptyStateTitle = BuildEmptyStateTitle(snapshot);
                EmptyStateMessage = BuildEmptyStateMessage(snapshot);
                SetSelectedDistro(null);
                return;
            }

            EmptyStateTitle = string.Empty;
            EmptyStateMessage = string.Empty;

            var selectedName = ResolveSelectedDistroName(preferredDistroName, snapshot.EnvironmentStatus.DefaultDistroName);
            SetSelectedDistro(Distros.FirstOrDefault(distro => string.Equals(distro.Name, selectedName, StringComparison.Ordinal)));
        }
        catch (OperationCanceledException) when (!pageRefreshCoordinator.IsLatest(lease.Version))
        {
        }
        catch (Exception ex)
        {
            if (!pageRefreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            IsLoading = false;
            HasLoaded = true;
            Distros = Array.Empty<DistroSelectionItem>();
            EmptyStateTitle = "Distro page unavailable";
            EmptyStateMessage = ex.Message;
            SummaryText = "CoolWSL could not load the distro inventory.";
            WarningText = ex.Message;
            SetSelectedDistro(null);
        }
    }

    public Task SelectDistroAsync(string? distroName)
    {
        var selectedItem = Distros.FirstOrDefault(distro => string.Equals(distro.Name, distroName, StringComparison.Ordinal));
        SetSelectedDistro(selectedItem);
        return Task.CompletedTask;
    }

    public Task OpenTerminalAsync()
        => RunActionAsync(
            cancellationToken => SelectedDistroName is null
                ? Task.FromResult(CommandResult.Failed(new WslCommand("wsl.exe", Array.Empty<string>()), DateTimeOffset.Now, DateTimeOffset.Now, string.Empty, string.Empty, null, new(WslErrorKind.DistroNotFound, "No distro is selected.")))
                : distroService.OpenDistroAsync(SelectedDistroName, cancellationToken),
            SelectedDistroName is null ? "No distro is selected." : $"Opened {SelectedDistroName} in a terminal.",
            refreshAfterSuccess: false);

    public Task StartDistroAsync()
        => RunMutationAsync(name => distroService.StartDistroAsync(name), "Started");

    public Task TerminateDistroAsync()
        => RunMutationAsync(name => distroService.TerminateDistroAsync(name), "Terminated");

    public Task SetDefaultDistroAsync()
        => RunMutationAsync(name => distroService.SetDefaultDistroAsync(name), "Set as default");

    private Task RunMutationAsync(Func<string, Task<CommandResult>> action, string actionVerb)
    {
        if (string.IsNullOrWhiteSpace(SelectedDistroName))
        {
            ActionStatusText = "Select a distro before running a lifecycle action.";
            return Task.CompletedTask;
        }

        return RunActionAsync(
            _ => action(SelectedDistroName),
            $"{actionVerb} {SelectedDistroName}.",
            refreshAfterSuccess: true);
    }

    private async Task RunActionAsync(
        Func<CancellationToken, Task<CommandResult>> action,
        string successMessage,
        bool refreshAfterSuccess)
    {
        // WSL lifecycle operations are not safely cancellable mid-flight;
        // partial execution could leave WSL in an inconsistent state.
        var result = await action(CancellationToken.None);
        ActionStatusText = result.IsSuccess
            ? successMessage
            : result.Error?.Summary ?? "The distro action failed.";

        if (refreshAfterSuccess && !string.IsNullOrWhiteSpace(SelectedDistroName))
        {
            await RefreshAsync(SelectedDistroName);
        }
    }

    private string ResolveSelectedDistroName(string? preferredDistroName, string? defaultDistroName)
    {
        if (!string.IsNullOrWhiteSpace(preferredDistroName) && Distros.Any(distro => string.Equals(distro.Name, preferredDistroName, StringComparison.Ordinal)))
        {
            return preferredDistroName;
        }

        if (!string.IsNullOrWhiteSpace(SelectedDistroName) && Distros.Any(distro => string.Equals(distro.Name, SelectedDistroName, StringComparison.Ordinal)))
        {
            return SelectedDistroName;
        }

        if (!string.IsNullOrWhiteSpace(defaultDistroName) && Distros.Any(distro => string.Equals(distro.Name, defaultDistroName, StringComparison.Ordinal)))
        {
            return defaultDistroName;
        }

        return Distros[0].Name;
    }

    private static readonly string[] SelectionPropertyNames =
    [
        nameof(SelectedDistro),
        nameof(SelectedDistroName),
        nameof(HeaderName),
        nameof(HeaderState),
        nameof(HeaderWslVersion),
        nameof(HeaderDefault),
        nameof(HeaderManagementLabel),
        nameof(HeaderMetadataText),
        nameof(HeaderCapabilityMessage),
        nameof(HasSelection),
        nameof(ShowEmptyState),
        nameof(HasDefaultLabel),
        nameof(HasManagementLabel),
        nameof(CanOpenTerminal),
        nameof(CanRunCommand),
        nameof(CanStart),
        nameof(CanTerminate),
        nameof(CanSetDefault),
    ];

    private void SetSelectedDistro(DistroSelectionItem? value)
    {
        selectedDistro = value;
        Settings.SetSelectedDistro(value?.Name);
        Diagnostics.SetSelectedDistro(value?.Name);

        foreach (var name in SelectionPropertyNames)
        {
            OnPropertyChanged(name);
        }
    }

    private static string BuildSummary(DashboardStatusSnapshot snapshot)
    {
        if (snapshot.DistroInventory.Distros.Count == 0)
        {
            return snapshot.DistroInventory.Summary;
        }

        var runningCount = snapshot.DistroInventory.Distros.Count(static distro => distro.IsRunning);
        return $"{snapshot.DistroInventory.Distros.Count} distros loaded. {runningCount} running.";
    }

    private static string BuildEmptyStateTitle(DashboardStatusSnapshot snapshot)
    {
        return snapshot.EnvironmentStatus.Availability switch
        {
            WslAvailability.NotInstalled => "WSL is not installed",
            WslAvailability.Unavailable => "WSL is unavailable",
            _ => "No distros installed",
        };
    }

    private static string BuildEmptyStateMessage(DashboardStatusSnapshot snapshot)
    {
        return snapshot.EnvironmentStatus.Availability switch
        {
            WslAvailability.NotInstalled => snapshot.EnvironmentStatus.Summary,
            WslAvailability.Unavailable => snapshot.EnvironmentStatus.Summary,
            _ => "Install a Linux distribution to populate the per-distro view.",
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

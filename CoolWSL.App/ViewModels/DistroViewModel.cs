using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.App.Models;
using CoolWSL.App.Services;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Models;
using CoolWSL.Diagnostics.Services;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.App.ViewModels;

public sealed class DistroViewModel : INotifyPropertyChanged
{
    private readonly IDashboardStatusService dashboardStatusService;
    private readonly IWslDistroService distroService;
    private readonly IDiagnosticsService diagnosticsService;
    private readonly RefreshCoordinator diagnosticsRefreshCoordinator = new();
    private readonly RefreshCoordinator pageRefreshCoordinator = new();
    private string actionStatusText = string.Empty;
    private IReadOnlyList<DiagnosticResult> diagnosticsResults = Array.Empty<DiagnosticResult>();
    private string diagnosticsStatusText = "Select a distro to load diagnostics.";
    private IReadOnlyList<DistroSelectionItem> distros = Array.Empty<DistroSelectionItem>();
    private string emptyStateMessage = "Refresh the distro page to load the current distro inventory.";
    private string emptyStateTitle = "No distro selected";
    private bool hasLoaded;
    private bool isDiagnosticsLoading;
    private bool isLoading;
    private DateTimeOffset? lastUpdatedAt;
    private DistroSelectionItem? selectedDistro;
    private string summaryText = "Select a distro to inspect lifecycle actions, commands, and diagnostics.";
    private string warningText = string.Empty;

    public DistroViewModel(
        IDashboardStatusService dashboardStatusService,
        IWslDistroService distroService,
        IDiagnosticsService diagnosticsService,
        CommandRunnerViewModel commandRunner)
    {
        this.dashboardStatusService = dashboardStatusService;
        this.distroService = distroService;
        this.diagnosticsService = diagnosticsService;
        CommandRunner = commandRunner;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CommandRunnerViewModel CommandRunner { get; }

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

    public IReadOnlyList<DiagnosticResult> DiagnosticsResults
    {
        get => diagnosticsResults;
        private set
        {
            diagnosticsResults = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDiagnosticsResults));
        }
    }

    public bool HasDiagnosticsResults => DiagnosticsResults.Count > 0;

    public bool IsDiagnosticsLoading
    {
        get => isDiagnosticsLoading;
        private set
        {
            if (isDiagnosticsLoading == value)
            {
                return;
            }

            isDiagnosticsLoading = value;
            OnPropertyChanged();
        }
    }

    public string DiagnosticsStatusText
    {
        get => diagnosticsStatusText;
        private set
        {
            if (string.Equals(diagnosticsStatusText, value, StringComparison.Ordinal))
            {
                return;
            }

            diagnosticsStatusText = value;
            OnPropertyChanged();
        }
    }

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
            WarningText = CombineDistinct(snapshot.EnvironmentStatus.DegradedReason, snapshot.DistroInventory.DegradedReason);
            lastUpdatedAt = DateTimeOffset.Now;
            OnPropertyChanged(nameof(RefreshStatusText));
            HasLoaded = true;
            IsLoading = false;

            if (Distros.Count == 0)
            {
                EmptyStateTitle = BuildEmptyStateTitle(snapshot);
                EmptyStateMessage = BuildEmptyStateMessage(snapshot);
                SetSelectedDistro(null);
                DiagnosticsResults = Array.Empty<DiagnosticResult>();
                DiagnosticsStatusText = "Install or select a distro to run diagnostics.";
                return;
            }

            EmptyStateTitle = string.Empty;
            EmptyStateMessage = string.Empty;

            var selectedName = ResolveSelectedDistroName(preferredDistroName, snapshot.EnvironmentStatus.DefaultDistroName);
            SetSelectedDistro(Distros.FirstOrDefault(distro => string.Equals(distro.Name, selectedName, StringComparison.Ordinal)));
            await RefreshDiagnosticsAsync();
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
            DiagnosticsResults = Array.Empty<DiagnosticResult>();
            DiagnosticsStatusText = "Diagnostics are unavailable until the distro inventory loads.";
        }
    }

    public Task SelectDistroAsync(string? distroName)
    {
        var selectedItem = Distros.FirstOrDefault(distro => string.Equals(distro.Name, distroName, StringComparison.Ordinal));
        SetSelectedDistro(selectedItem);
        return RefreshDiagnosticsAsync();
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

    public async Task RefreshDiagnosticsAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedDistroName))
        {
            DiagnosticsResults = Array.Empty<DiagnosticResult>();
            DiagnosticsStatusText = "Select a distro to run diagnostics.";
            return;
        }

        var lease = diagnosticsRefreshCoordinator.Start();
        IsDiagnosticsLoading = true;

        try
        {
            var snapshot = await diagnosticsService.GetSnapshotAsync(SelectedDistroName, lease.CancellationToken);
            if (!diagnosticsRefreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            DiagnosticsResults = snapshot.Results;
            DiagnosticsStatusText = BuildDiagnosticsSummary(snapshot.Results);
        }
        catch (OperationCanceledException) when (!diagnosticsRefreshCoordinator.IsLatest(lease.Version))
        {
        }
        catch (Exception ex)
        {
            if (!diagnosticsRefreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            DiagnosticsResults = Array.Empty<DiagnosticResult>();
            DiagnosticsStatusText = ex.Message;
        }
        finally
        {
            if (diagnosticsRefreshCoordinator.IsLatest(lease.Version))
            {
                IsDiagnosticsLoading = false;
            }
        }
    }

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

    private void SetSelectedDistro(DistroSelectionItem? value)
    {
        selectedDistro = value;
        CommandRunner.SetSelectedDistro(value?.Name);
        OnPropertyChanged(nameof(SelectedDistro));
        OnPropertyChanged(nameof(SelectedDistroName));
        OnPropertyChanged(nameof(HeaderName));
        OnPropertyChanged(nameof(HeaderState));
        OnPropertyChanged(nameof(HeaderWslVersion));
        OnPropertyChanged(nameof(HeaderDefault));
        OnPropertyChanged(nameof(HeaderManagementLabel));
        OnPropertyChanged(nameof(HeaderCapabilityMessage));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(HasDefaultLabel));
        OnPropertyChanged(nameof(HasManagementLabel));
        OnPropertyChanged(nameof(CanOpenTerminal));
        OnPropertyChanged(nameof(CanRunCommand));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanTerminate));
        OnPropertyChanged(nameof(CanSetDefault));
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

    private static string BuildDiagnosticsSummary(IReadOnlyList<DiagnosticResult> results)
    {
        if (results.Count == 0)
        {
            return "No diagnostics have been loaded yet.";
        }

        var warningCount = results.Count(result => result.Severity == DiagnosticSeverity.Warning);
        var errorCount = results.Count(result => result.Severity == DiagnosticSeverity.Error);
        return $"{results.Count} diagnostics loaded. {warningCount} warning{(warningCount == 1 ? string.Empty : "s")}, {errorCount} error{(errorCount == 1 ? string.Empty : "s")}.";
    }

    private static string CombineDistinct(params string?[] values)
        => string.Join(
            " ",
            values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value!.Trim()).Distinct(StringComparer.Ordinal));

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.App.Models;
using CoolWSL.App.Services;
using CoolWSL.Diagnostics.Models;
using CoolWSL.Diagnostics.Services;

namespace CoolWSL.App.ViewModels;

public sealed class DiagnosticsViewModel : INotifyPropertyChanged
{
    private readonly IDiagnosticsService diagnosticsService;
    private readonly RefreshCoordinator refreshCoordinator = new();
    private IReadOnlyList<DistroSelectionItem> distros = Array.Empty<DistroSelectionItem>();
    private bool hasLoaded;
    private bool isLoading;
    private DateTimeOffset? lastUpdatedAt;
    private IReadOnlyList<DiagnosticResult> results = Array.Empty<DiagnosticResult>();
    private string selectedDistroName = string.Empty;
    private string summaryText = "Refresh diagnostics to inspect WSL health and per-distro checks.";
    private string warningText = string.Empty;

    public DiagnosticsViewModel(IDiagnosticsService diagnosticsService)
    {
        this.diagnosticsService = diagnosticsService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<DistroSelectionItem> Distros
    {
        get => distros;
        private set
        {
            distros = value;
            OnPropertyChanged();
        }
    }

    public string SelectedDistroName => selectedDistroName;

    public DistroSelectionItem? SelectedDistro => Distros.FirstOrDefault(distro => string.Equals(distro.Name, selectedDistroName, StringComparison.Ordinal));

    public IReadOnlyList<DiagnosticResult> Results
    {
        get => results;
        private set
        {
            results = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasResults));
        }
    }

    public bool HasResults => Results.Count > 0;

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

    public string RefreshStatusText
        => lastUpdatedAt.HasValue
            ? $"Last refreshed {lastUpdatedAt.Value.LocalDateTime:g}."
            : "Diagnostics have not been loaded yet.";

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

    public async Task EnsureLoadedAsync()
    {
        if (HasLoaded)
        {
            return;
        }

        await RefreshAsync();
    }

    public async Task RefreshAsync(string? preferredDistroName = null)
    {
        var lease = refreshCoordinator.Start();
        IsLoading = true;

        try
        {
            var snapshot = await diagnosticsService.GetSnapshotAsync(preferredDistroName ?? selectedDistroName, lease.CancellationToken);
            if (!refreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            Distros = snapshot.DistroInventory.Distros.Select(DistroSelectionItem.Create).ToArray();
            selectedDistroName = snapshot.SelectedDistroName ?? string.Empty;
            OnPropertyChanged(nameof(SelectedDistroName));
            OnPropertyChanged(nameof(SelectedDistro));
            Results = snapshot.Results;
            SummaryText = BuildSummary(snapshot.Results, selectedDistroName);
            WarningText = string.Join(
                " ",
                snapshot.Results
                    .Where(result => result.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
                    .Select(result => result.Summary)
                    .Distinct(StringComparer.Ordinal));
            lastUpdatedAt = DateTimeOffset.Now;
            OnPropertyChanged(nameof(RefreshStatusText));
            HasLoaded = true;
        }
        catch (OperationCanceledException) when (!refreshCoordinator.IsLatest(lease.Version))
        {
        }
        catch (Exception ex)
        {
            if (!refreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            Results = Array.Empty<DiagnosticResult>();
            SummaryText = "CoolWSL could not load diagnostics.";
            WarningText = ex.Message;
            Distros = Array.Empty<DistroSelectionItem>();
            selectedDistroName = string.Empty;
            OnPropertyChanged(nameof(SelectedDistroName));
            OnPropertyChanged(nameof(SelectedDistro));
            HasLoaded = true;
        }
        finally
        {
            if (refreshCoordinator.IsLatest(lease.Version))
            {
                IsLoading = false;
            }
        }
    }

    public Task SelectDistroAsync(string? distroName)
        => RefreshAsync(distroName);

    private static string BuildSummary(IReadOnlyList<DiagnosticResult> results, string selectedDistroName)
    {
        if (results.Count == 0)
        {
            return "No diagnostics have been loaded yet.";
        }

        var warningCount = results.Count(result => result.Severity == DiagnosticSeverity.Warning);
        var errorCount = results.Count(result => result.Severity == DiagnosticSeverity.Error);
        var distroContext = string.IsNullOrWhiteSpace(selectedDistroName)
            ? "No distro-specific checks were selected."
            : $"Distro-specific checks targeted {selectedDistroName}.";
        return $"{results.Count} diagnostics loaded. {warningCount} warning{(warningCount == 1 ? string.Empty : "s")}, {errorCount} error{(errorCount == 1 ? string.Empty : "s")}. {distroContext}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
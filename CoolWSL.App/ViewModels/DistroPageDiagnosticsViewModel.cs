using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.App.Services;
using CoolWSL.Diagnostics.Models;
using CoolWSL.Diagnostics.Services;

namespace CoolWSL.App.ViewModels;

public sealed class DistroPageDiagnosticsViewModel : INotifyPropertyChanged
{
    private readonly IDiagnosticsService diagnosticsService;
    private readonly RefreshCoordinator refreshCoordinator = new();
    private IReadOnlyList<DiagnosticResult> results = Array.Empty<DiagnosticResult>();
    private bool isLoading;
    private string statusText = "Select a distro to load diagnostics.";
    private string? selectedDistroName;

    public DistroPageDiagnosticsViewModel(IDiagnosticsService diagnosticsService)
    {
        this.diagnosticsService = diagnosticsService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public string StatusText
    {
        get => statusText;
        private set
        {
            if (string.Equals(statusText, value, StringComparison.Ordinal))
            {
                return;
            }

            statusText = value;
            OnPropertyChanged();
        }
    }

    public void SetSelectedDistro(string? distroName)
    {
        selectedDistroName = distroName;
    }

    public async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(selectedDistroName))
        {
            Results = Array.Empty<DiagnosticResult>();
            StatusText = "Select a distro to run diagnostics.";
            return;
        }

        var lease = refreshCoordinator.Start();
        IsLoading = true;

        try
        {
            var snapshot = await diagnosticsService.GetSnapshotAsync(selectedDistroName, lease.CancellationToken);
            if (!refreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            Results = snapshot.Results;
            StatusText = BuildSummary(snapshot.Results);
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
            StatusText = ex.Message;
        }
        finally
        {
            if (refreshCoordinator.IsLatest(lease.Version))
            {
                IsLoading = false;
            }
        }
    }

    private static string BuildSummary(IReadOnlyList<DiagnosticResult> results)
    {
        if (results.Count == 0)
        {
            return "No diagnostics have been loaded yet.";
        }

        var warningCount = results.Count(result => result.Severity == DiagnosticSeverity.Warning);
        var errorCount = results.Count(result => result.Severity == DiagnosticSeverity.Error);
        return $"{results.Count} diagnostics loaded. {warningCount} warning{(warningCount == 1 ? string.Empty : "s")}, {errorCount} error{(errorCount == 1 ? string.Empty : "s")}.";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

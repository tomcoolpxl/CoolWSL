using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;

namespace CoolWSL.App.ViewModels;

public sealed class CommandRunnerViewModel : INotifyPropertyChanged
{
    private readonly IWslDistroService distroService;
    private readonly Dictionary<string, List<CommandHistoryEntry>> historyByDistro = new(StringComparer.Ordinal);
    private CancellationTokenSource? currentRunSource;
    private string commandText = string.Empty;
    private IReadOnlyList<CommandHistoryEntry> history = Array.Empty<CommandHistoryEntry>();
    private bool isRunning;
    private string selectedDistroName = string.Empty;
    private string standardError = string.Empty;
    private string standardOutput = string.Empty;
    private string statusText = "Select a distro and enter a command to begin.";
    private double timeoutSeconds = 30;
    private string exitCodeText = "n/a";

    public CommandRunnerViewModel(IWslDistroService distroService)
    {
        this.distroService = distroService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SelectedDistroName => selectedDistroName;

    public string CommandText
    {
        get => commandText;
        set
        {
            if (string.Equals(commandText, value, StringComparison.Ordinal))
            {
                return;
            }

            commandText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRun));
        }
    }

    public double TimeoutSeconds
    {
        get => timeoutSeconds;
        set
        {
            var normalizedValue = value < 1 ? 1 : value;
            if (Math.Abs(timeoutSeconds - normalizedValue) < double.Epsilon)
            {
                return;
            }

            timeoutSeconds = normalizedValue;
            OnPropertyChanged();
        }
    }

    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            if (isRunning == value)
            {
                return;
            }

            isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(CanCancel));
        }
    }

    public bool CanRun =>
        !IsRunning &&
        !string.IsNullOrWhiteSpace(selectedDistroName) &&
        !string.IsNullOrWhiteSpace(CommandText);

    public bool CanCancel => IsRunning;

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

    public string StandardOutput
    {
        get => standardOutput;
        private set
        {
            if (string.Equals(standardOutput, value, StringComparison.Ordinal))
            {
                return;
            }

            standardOutput = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStandardOutput));
        }
    }

    public string StandardError
    {
        get => standardError;
        private set
        {
            if (string.Equals(standardError, value, StringComparison.Ordinal))
            {
                return;
            }

            standardError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStandardError));
        }
    }

    public bool HasStandardOutput => !string.IsNullOrWhiteSpace(StandardOutput);

    public bool HasStandardError => !string.IsNullOrWhiteSpace(StandardError);

    public string ExitCodeText
    {
        get => exitCodeText;
        private set
        {
            if (string.Equals(exitCodeText, value, StringComparison.Ordinal))
            {
                return;
            }

            exitCodeText = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<CommandHistoryEntry> History
    {
        get => history;
        private set
        {
            history = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasHistory));
        }
    }

    public bool HasHistory => History.Count > 0;

    public void SetSelectedDistro(string? distroName)
    {
        selectedDistroName = distroName?.Trim() ?? string.Empty;
        OnPropertyChanged(nameof(SelectedDistroName));
        OnPropertyChanged(nameof(CanRun));

        StandardOutput = string.Empty;
        StandardError = string.Empty;
        ExitCodeText = "n/a";
        History = LoadHistory(selectedDistroName);
        StatusText = string.IsNullOrWhiteSpace(selectedDistroName)
            ? "Select a distro and enter a command to begin."
            : $"Ready to run commands inside {selectedDistroName}.";
    }

    public async Task RunAsync()
    {
        if (!CanRun)
        {
            StatusText = string.IsNullOrWhiteSpace(selectedDistroName)
                ? "Select a distro before running a command."
                : "Enter a command before running it.";
            return;
        }

        currentRunSource?.Cancel();
        currentRunSource?.Dispose();
        currentRunSource = new CancellationTokenSource();

        IsRunning = true;
        StandardOutput = string.Empty;
        StandardError = string.Empty;
        ExitCodeText = "n/a";
        StatusText = $"Running command in {selectedDistroName}...";

        try
        {
            var timeout = TimeSpan.FromSeconds(Math.Max(1, TimeoutSeconds));
            var result = await distroService.RunInDistroAsync(selectedDistroName, CommandText.Trim(), timeout, currentRunSource.Token);

            StandardOutput = result.StandardOutput;
            StandardError = result.StandardError;
            ExitCodeText = result.ExitCode?.ToString() ?? "n/a";
            StatusText = result.IsSuccess
                ? $"Command succeeded in {selectedDistroName}."
                : result.Error?.Summary ?? $"Command failed in {selectedDistroName}.";

            AddHistory(new(
                selectedDistroName,
                CommandText.Trim(),
                result.Status,
                result.StartedAt,
                result.EndedAt,
                result.ExitCode,
                result.StandardOutput,
                result.StandardError));
        }
        finally
        {
            currentRunSource?.Dispose();
            currentRunSource = null;
            IsRunning = false;
        }
    }

    public void Cancel()
    {
        if (!IsRunning)
        {
            return;
        }

        StatusText = $"Cancelling the command running in {selectedDistroName}...";
        currentRunSource?.Cancel();
    }

    public void ReuseHistoryEntry(CommandHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        CommandText = entry.CommandText;
    }

    private IReadOnlyList<CommandHistoryEntry> LoadHistory(string distroName)
    {
        if (string.IsNullOrWhiteSpace(distroName) || !historyByDistro.TryGetValue(distroName, out var entries))
        {
            return Array.Empty<CommandHistoryEntry>();
        }

        return entries.ToArray();
    }

    private const int MaxHistoryPerDistro = 50;

    private void AddHistory(CommandHistoryEntry entry)
    {
        if (!historyByDistro.TryGetValue(entry.DistroName, out var entries))
        {
            entries = [];
            historyByDistro[entry.DistroName] = entries;
        }

        entries.Insert(0, entry);

        if (entries.Count > MaxHistoryPerDistro)
        {
            entries.RemoveRange(MaxHistoryPerDistro, entries.Count - MaxHistoryPerDistro);
        }

        History = entries.ToArray();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
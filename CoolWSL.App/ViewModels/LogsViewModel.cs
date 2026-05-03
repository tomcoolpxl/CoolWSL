using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.App.Models;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;

namespace CoolWSL.App.ViewModels;

public sealed class LogsViewModel : INotifyPropertyChanged
{
    private readonly IAppLogReader logReader;
    private string summaryText = "No metadata log entries loaded.";
    private string lastLoadedText = "Not refreshed yet.";

    public LogsViewModel(IAppLogReader logReader)
    {
        this.logReader = logReader;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LogEntryRow> Entries { get; } = [];

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

    public bool HasEntries => Entries.Count > 0;

    public bool ShowEmptyState => !HasEntries;

    public void Refresh(string levelFilter, string searchText)
    {
        var allEntries = logReader.GetEntries();
        var filteredEntries = Filter(allEntries, levelFilter, searchText).ToArray();

        Entries.Clear();
        foreach (var entry in filteredEntries)
        {
            Entries.Add(new LogEntryRow(entry));
        }

        SummaryText = allEntries.Count == 0
            ? "No metadata log entries have been captured in this app session yet."
            : $"{filteredEntries.Length} of {allEntries.Count} metadata log entries shown.";
        LastLoadedText = $"Refreshed {DateTimeOffset.Now:t}";

        OnPropertyChanged(nameof(HasEntries));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    public void Clear(string levelFilter, string searchText)
    {
        logReader.ClearDisplayedEntries();
        Refresh(levelFilter, searchText);
    }

    private static IEnumerable<AppLogEntry> Filter(
        IEnumerable<AppLogEntry> entries,
        string levelFilter,
        string searchText)
    {
        var query = entries;

        if (!string.IsNullOrWhiteSpace(levelFilter) &&
            !string.Equals(levelFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(entry => string.Equals(entry.Level, levelFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(entry =>
                entry.Area.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                entry.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

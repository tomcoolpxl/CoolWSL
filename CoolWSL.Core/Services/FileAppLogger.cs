using System.Text.Json;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;

namespace CoolWSL.Core.Services;

public sealed class FileAppLogger : IAppLogger, IAppLogReader
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);
    private const int MaximumEntriesToRead = 1_000;

    private readonly object gate = new();
    private readonly TimeProvider timeProvider;
    private readonly string logDirectory;
    private DateTimeOffset sessionStart;

    public FileAppLogger(TimeProvider timeProvider)
        : this(timeProvider, GetDefaultLogDirectory())
    {
    }

    public FileAppLogger(TimeProvider timeProvider, string logDirectory)
    {
        this.timeProvider = timeProvider;
        this.logDirectory = logDirectory;
        sessionStart = timeProvider.GetUtcNow();
    }

    public void LogInfo(string area, string message)
    {
        var now = timeProvider.GetUtcNow();
        var entry = new AppLogEntry(
            now,
            "Info",
            string.IsNullOrWhiteSpace(area) ? "Application" : area.Trim(),
            message ?? string.Empty);

        lock (gate)
        {
            Directory.CreateDirectory(logDirectory);
            PruneExpiredFiles(now);
            File.AppendAllText(GetLogPath(now), JsonSerializer.Serialize(entry) + Environment.NewLine);
        }
    }

    public IReadOnlyList<AppLogEntry> GetEntries()
    {
        lock (gate)
        {
            Directory.CreateDirectory(logDirectory);
            PruneExpiredFiles(timeProvider.GetUtcNow());

            var minTimestamp = sessionStart;
            return Directory
                .EnumerateFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly)
                .SelectMany(ReadEntries)
                .Where(IsWithinRetention)
                .Where(entry => entry.Timestamp >= minTimestamp)
                .OrderByDescending(static entry => entry.Timestamp)
                .Take(MaximumEntriesToRead)
                .ToArray();
        }
    }

    public void ClearDisplayedEntries()
    {
        lock (gate)
        {
            sessionStart = timeProvider.GetUtcNow();
        }
    }

    private IEnumerable<AppLogEntry> ReadEntries(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            AppLogEntry? entry = null;

            try
            {
                entry = JsonSerializer.Deserialize<AppLogEntry>(line);
            }
            catch (JsonException)
            {
            }

            if (entry is not null)
            {
                yield return entry;
            }
        }
    }

    private bool IsWithinRetention(AppLogEntry entry)
        => entry.Timestamp >= timeProvider.GetUtcNow() - RetentionPeriod;

    private void PruneExpiredFiles(DateTimeOffset now)
    {
        if (!Directory.Exists(logDirectory))
        {
            return;
        }

        var oldestAllowed = now - RetentionPeriod;
        foreach (var path in Directory.EnumerateFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly))
        {
            if (File.GetLastWriteTimeUtc(path) < oldestAllowed.UtcDateTime)
            {
                File.Delete(path);
            }
        }
    }

    private string GetLogPath(DateTimeOffset timestamp)
        => Path.Combine(logDirectory, $"{timestamp:yyyy-MM-dd}.log");

    private static string GetDefaultLogDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CoolWSL",
            "Logs");
}

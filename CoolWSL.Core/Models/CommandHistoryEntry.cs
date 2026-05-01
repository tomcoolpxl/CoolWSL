namespace CoolWSL.Core.Models;

public sealed record CommandHistoryEntry(
    string DistroName,
    string CommandText,
    CommandExecutionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    int? ExitCode,
    string StandardOutput,
    string StandardError)
{
    public TimeSpan Duration => EndedAt - StartedAt;

    public string ExitCodeLabel => ExitCode?.ToString() ?? "n/a";

    public string StatusLabel => Status switch
    {
        CommandExecutionStatus.Launched => "Launched",
        CommandExecutionStatus.Succeeded => "Succeeded",
        CommandExecutionStatus.Failed => "Failed",
        CommandExecutionStatus.TimedOut => "Timed out",
        CommandExecutionStatus.Cancelled => "Cancelled",
        CommandExecutionStatus.LaunchFailed => "Launch failed",
        _ => Status.ToString(),
    };
}
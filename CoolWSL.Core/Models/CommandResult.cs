namespace CoolWSL.Core.Models;

public sealed class CommandResult
{
    private CommandResult(
        WslCommand command,
        CommandExecutionStatus status,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        string standardOutput,
        string standardError,
        int? exitCode,
        WslCommandError? error)
    {
        Command = command;
        Status = status;
        StartedAt = startedAt;
        EndedAt = endedAt;
        StandardOutput = standardOutput;
        StandardError = standardError;
        ExitCode = exitCode;
        Error = error;
    }

    public WslCommand Command { get; }

    public CommandExecutionStatus Status { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset EndedAt { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public int? ExitCode { get; }

    public WslCommandError? Error { get; }

    public TimeSpan Duration => EndedAt - StartedAt;

    public bool IsSuccess =>
        Status == CommandExecutionStatus.Succeeded ||
        Status == CommandExecutionStatus.Launched;

    public static CommandResult Launched(
        WslCommand command,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt)
        => new(command, CommandExecutionStatus.Launched, startedAt, endedAt, string.Empty, string.Empty, null, null);

    public static CommandResult Succeeded(
        WslCommand command,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        string standardOutput,
        string standardError,
        int exitCode)
        => new(command, CommandExecutionStatus.Succeeded, startedAt, endedAt, standardOutput, standardError, exitCode, null);

    public static CommandResult Failed(
        WslCommand command,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        string standardOutput,
        string standardError,
        int? exitCode,
        WslCommandError error)
        => new(command, CommandExecutionStatus.Failed, startedAt, endedAt, standardOutput, standardError, exitCode, error);

    public static CommandResult TimedOut(
        WslCommand command,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        string standardOutput,
        string standardError,
        int? exitCode,
        WslCommandError error)
        => new(command, CommandExecutionStatus.TimedOut, startedAt, endedAt, standardOutput, standardError, exitCode, error);

    public static CommandResult Cancelled(
        WslCommand command,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        string standardOutput,
        string standardError,
        int? exitCode,
        WslCommandError error)
        => new(command, CommandExecutionStatus.Cancelled, startedAt, endedAt, standardOutput, standardError, exitCode, error);

    public static CommandResult LaunchFailed(
        WslCommand command,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        WslCommandError error)
        => new(command, CommandExecutionStatus.LaunchFailed, startedAt, endedAt, string.Empty, string.Empty, null, error);
}
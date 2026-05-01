namespace CoolWSL.Core.Models;

public enum CommandExecutionStatus
{
    Launched,
    Succeeded,
    Failed,
    TimedOut,
    Cancelled,
    LaunchFailed,
}
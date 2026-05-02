namespace CoolWSL.Core.Models.Configuration;

public enum WslConfigProbeStatus
{
    Unknown,
    Effective,
    NotEffective,
    Skipped
}

public sealed record WslConfigProbeResult(
    string ProbeId,
    string KeyId,
    WslConfigProbeStatus Status,
    string Evidence,
    string CommandAttempted,
    DateTimeOffset RunAt);

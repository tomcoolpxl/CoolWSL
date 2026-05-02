using CoolWSL.Core.Models;

namespace CoolWSL.Core.Models.Configuration;

public enum WslConfigRestartImpact
{
    None,
    NewWslSession,
    NewShell,
    TerminateDistro,
    ShutdownWsl
}

public sealed record WslDistroConfigSaveResult(
    string DistroName,
    string OnDiskPath,
    string? BackupPath,
    DateTimeOffset SavedAt,
    WslConfigValidationResult Validation,
    WslConfigRestartImpact RestartSuggestion);

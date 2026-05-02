namespace CoolWSL.Core.Models;

public sealed record WslGlobalConfigSaveResult(
    string Path,
    string? BackupPath,
    DateTimeOffset SavedAt,
    WslConfigValidationResult Validation);

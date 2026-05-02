namespace CoolWSL.Core.Models;

public sealed record WslGlobalConfigDocument(
    string Path,
    bool Exists,
    string Content,
    DateTimeOffset LoadedAt,
    WslConfigValidationResult Validation);

using CoolWSL.Core.Models;

namespace CoolWSL.Core.Models.Configuration;

public sealed record WslDistroConfigDocument(
    string DistroName,
    IniDocument Document,
    string OriginalContent,
    bool Existed,
    DateTimeOffset LoadedAt,
    WslConfigValidationResult Validation);

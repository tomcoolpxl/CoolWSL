namespace CoolWSL.Core.Models;

public sealed record WslConfigValidationResult(IReadOnlyList<WslConfigValidationIssue> Issues)
{
    public bool HasErrors => Issues.Any(static issue => issue.Severity == WslConfigValidationSeverity.Error);

    public bool HasWarnings => Issues.Any(static issue => issue.Severity == WslConfigValidationSeverity.Warning);

    public static WslConfigValidationResult Empty { get; } = new(Array.Empty<WslConfigValidationIssue>());
}

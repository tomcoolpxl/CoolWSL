namespace CoolWSL.Core.Models;

public sealed record WslConfigValidationIssue(
    WslConfigValidationSeverity Severity,
    string Message,
    int? LineNumber = null);

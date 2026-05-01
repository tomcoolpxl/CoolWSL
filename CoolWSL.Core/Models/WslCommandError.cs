namespace CoolWSL.Core.Models;

public sealed record WslCommandError(WslErrorKind Kind, string Summary, string? SuggestedNextStep = null);
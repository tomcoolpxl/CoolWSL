namespace CoolWSL.Diagnostics.Models;

public sealed record DiagnosticResult(
    string Id,
    string Title,
    DiagnosticSeverity Severity,
    string Summary,
    string Details,
    string RawOutput,
    string? SuggestedNextStep = null,
    string? CommandText = null)
{
    public string SeverityLabel => Severity switch
    {
        DiagnosticSeverity.Success => "Healthy",
        DiagnosticSeverity.Info => "Info",
        DiagnosticSeverity.Warning => "Warning",
        DiagnosticSeverity.Error => "Error",
        _ => Severity.ToString(),
    };

    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    public bool HasRawOutput => !string.IsNullOrWhiteSpace(RawOutput);

    public bool HasSuggestedNextStep => !string.IsNullOrWhiteSpace(SuggestedNextStep);

    public bool HasCommandText => !string.IsNullOrWhiteSpace(CommandText);
}
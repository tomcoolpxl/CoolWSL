namespace CoolWSL.Core.Models;

public sealed record OperationRequest(
    string Title,
    string ConfirmButtonText,
    string TargetText,
    string ImpactText,
    string? DetailText = null)
{
    public bool HasDetailText => !string.IsNullOrWhiteSpace(DetailText);
}
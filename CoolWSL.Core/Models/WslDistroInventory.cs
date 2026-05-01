namespace CoolWSL.Core.Models;

public sealed record WslDistroInventory(
    WslAvailability Availability,
    IReadOnlyList<WslDistro> Distros,
    string Summary,
    bool IsDegraded = false,
    string? DegradedReason = null,
    string? SuggestedNextStep = null);
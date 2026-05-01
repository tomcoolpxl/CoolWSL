namespace CoolWSL.Core.Models;

public sealed record WslEnvironmentStatus(
    WslAvailability Availability,
    string Summary,
    string? DefaultDistroName,
    int? DefaultWslVersion,
    string? KernelVersion,
    string? WslVersion,
    string? WindowsVersion,
    bool IsDegraded = false,
    string? DegradedReason = null,
    string? SuggestedNextStep = null);
using CoolWSL.Core.Models;

namespace CoolWSL.Diagnostics.Models;

public sealed record DiagnosticsSnapshot(
    WslEnvironmentStatus EnvironmentStatus,
    WslDistroInventory DistroInventory,
    string? SelectedDistroName,
    IReadOnlyList<DiagnosticResult> Results);
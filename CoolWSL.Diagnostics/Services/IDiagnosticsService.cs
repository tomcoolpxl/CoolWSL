using CoolWSL.Diagnostics.Models;

namespace CoolWSL.Diagnostics.Services;

public interface IDiagnosticsService
{
    Task<DiagnosticsSnapshot> GetSnapshotAsync(string? selectedDistroName, CancellationToken cancellationToken = default);
}
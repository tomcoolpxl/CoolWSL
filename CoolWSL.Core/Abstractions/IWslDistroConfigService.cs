using CoolWSL.Core.Models;
using CoolWSL.Core.Models.Configuration;

namespace CoolWSL.Core.Abstractions;

public interface IWslDistroConfigService
{
    Task<WslDistroConfigDocument> ReadAsync(
        string distroName,
        CancellationToken cancellationToken = default);

    WslConfigValidationResult Validate(
        IniDocument document,
        WslDistroCapabilityContext capabilities);

    Task<WslDistroConfigSaveResult> SaveAsync(
        string distroName,
        IniDocument document,
        WslDistroCapabilityContext capabilities,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WslConfigProbeResult>> ProbeAsync(
        string distroName,
        IniDocument document,
        CancellationToken cancellationToken = default);

    Task<WslDistroConfigDeleteResult> RestoreDefaultsAsync(
        string distroName,
        CancellationToken cancellationToken = default);
}

public sealed record WslDistroConfigDeleteResult(
    string DistroName,
    string FilePath,
    bool DidExist,
    string? BackupPath,
    bool IsSuccess,
    string? ErrorMessage,
    DateTimeOffset CompletedAt);

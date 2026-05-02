using CoolWSL.Core.Models;

namespace CoolWSL.Core.Abstractions;

public sealed record DistroFileReadResult(string Content, bool Exists, WslCommandError? Error);
public sealed record DistroFileWriteResult(bool IsSuccess, WslCommandError? Error);

public interface IWslDistroFileService
{
    Task<DistroFileReadResult> ReadTextAsync(
        string distroName,
        string linuxPath,
        bool readAsRoot = false,
        CancellationToken cancellationToken = default);

    Task<DistroFileWriteResult> WriteTextAsync(
        string distroName,
        string linuxPath,
        string contents,
        bool writeAsRoot = true,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string distroName,
        string linuxPath,
        CancellationToken cancellationToken = default);
}

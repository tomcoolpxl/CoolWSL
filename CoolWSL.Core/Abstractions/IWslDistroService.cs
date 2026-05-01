using CoolWSL.Core.Models;

namespace CoolWSL.Core.Abstractions;

public interface IWslDistroService
{
    Task<WslDistroInventory> GetDistroInventoryAsync(CancellationToken cancellationToken = default);

    Task<WslEnvironmentStatus> GetEnvironmentStatusAsync(CancellationToken cancellationToken = default);

    Task<CommandResult> OpenDefaultDistroAsync(CancellationToken cancellationToken = default);

    Task<CommandResult> OpenDistroAsync(string distroName, CancellationToken cancellationToken = default);

    Task<CommandResult> StartDistroAsync(string distroName, CancellationToken cancellationToken = default);

    Task<CommandResult> TerminateDistroAsync(string distroName, CancellationToken cancellationToken = default);

    Task<CommandResult> SetDefaultDistroAsync(string distroName, CancellationToken cancellationToken = default);

    Task<CommandResult> ShutdownAsync(CancellationToken cancellationToken = default);

    Task<CommandResult> RunInDistroAsync(string distroName, string commandText, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
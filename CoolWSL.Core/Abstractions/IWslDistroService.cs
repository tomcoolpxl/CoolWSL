using CoolWSL.Core.Models;

namespace CoolWSL.Core.Abstractions;

public interface IWslDistroService
{
    Task<WslDistroInventory> GetDistroInventoryAsync(CancellationToken cancellationToken = default);

    Task<WslEnvironmentStatus> GetEnvironmentStatusAsync(CancellationToken cancellationToken = default);
}
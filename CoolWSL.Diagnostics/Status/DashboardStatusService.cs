using CoolWSL.Core.Abstractions;

namespace CoolWSL.Diagnostics.Status;

public sealed class DashboardStatusService : IDashboardStatusService
{
    private readonly IWslDistroService distroService;

    public DashboardStatusService(IWslDistroService distroService)
    {
        this.distroService = distroService;
    }

    public async Task<DashboardStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var environmentTask = distroService.GetEnvironmentStatusAsync(cancellationToken);
        var inventoryTask = distroService.GetDistroInventoryAsync(cancellationToken);

        await Task.WhenAll(environmentTask, inventoryTask).ConfigureAwait(false);

        return new(environmentTask.Result, inventoryTask.Result);
    }
}
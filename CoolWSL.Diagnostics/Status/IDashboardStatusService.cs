namespace CoolWSL.Diagnostics.Status;

public interface IDashboardStatusService
{
    Task<DashboardStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
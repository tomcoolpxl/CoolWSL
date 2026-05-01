namespace CoolWSL.App.Services;

public sealed class RefreshCoordinator : IDisposable
{
    private readonly object gate = new();
    private CancellationTokenSource? currentRefreshSource;
    private long currentVersion;

    public RefreshLease Start()
    {
        lock (gate)
        {
            currentRefreshSource?.Cancel();
            currentRefreshSource?.Dispose();
            currentRefreshSource = new CancellationTokenSource();
            currentVersion++;

            return new(currentVersion, currentRefreshSource.Token);
        }
    }

    public bool IsLatest(long version)
    {
        lock (gate)
        {
            return version == currentVersion;
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            currentRefreshSource?.Cancel();
            currentRefreshSource?.Dispose();
            currentRefreshSource = null;
        }
    }
}

public readonly record struct RefreshLease(long Version, CancellationToken CancellationToken);
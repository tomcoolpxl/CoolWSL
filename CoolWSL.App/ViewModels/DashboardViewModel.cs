using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.App.Models;
using CoolWSL.App.Services;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.App.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IDashboardStatusService dashboardStatusService;
    private readonly RefreshCoordinator refreshCoordinator;
    private DashboardState state = DashboardState.Initial;

    public DashboardViewModel(IDashboardStatusService dashboardStatusService, RefreshCoordinator refreshCoordinator)
    {
        this.dashboardStatusService = dashboardStatusService;
        this.refreshCoordinator = refreshCoordinator;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DashboardState State
    {
        get => state;
        private set
        {
            if (state == value)
            {
                return;
            }

            state = value;
            OnPropertyChanged();
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (State.HasLoaded || State.IsLoading)
        {
            return;
        }

        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var lease = refreshCoordinator.Start();
        State = State.WithLoading();

        try
        {
            var snapshot = await dashboardStatusService.GetSnapshotAsync(lease.CancellationToken);
            if (!refreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            State = DashboardState.Create(snapshot, DateTimeOffset.Now);
        }
        catch (OperationCanceledException) when (!refreshCoordinator.IsLatest(lease.Version))
        {
        }
        catch (Exception ex)
        {
            if (!refreshCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            State = State.WithRefreshFailure(ex.Message);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
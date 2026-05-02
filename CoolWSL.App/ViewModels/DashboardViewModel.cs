using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.App.Models;
using CoolWSL.App.Services;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.App.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IDashboardStatusService dashboardStatusService;
    private readonly IWslDistroService distroService;
    private readonly RefreshCoordinator refreshCoordinator = new();
    private string actionStatusText = string.Empty;
    private bool isActionInProgress;
    private DashboardState state = DashboardState.Initial;

    public DashboardViewModel(IDashboardStatusService dashboardStatusService, IWslDistroService distroService)
    {
        this.dashboardStatusService = dashboardStatusService;
        this.distroService = distroService;
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

    public string ActionStatusText
    {
        get => actionStatusText;
        private set
        {
            if (string.Equals(actionStatusText, value, StringComparison.Ordinal))
            {
                return;
            }

            actionStatusText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasActionStatus));
        }
    }

    public bool HasActionStatus => !string.IsNullOrWhiteSpace(ActionStatusText);

    public bool IsActionInProgress
    {
        get => isActionInProgress;
        private set
        {
            if (isActionInProgress == value)
            {
                return;
            }

            isActionInProgress = value;
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

    public Task OpenDefaultDistroAsync()
    {
        if (!State.HasDefaultDistro)
        {
            ActionStatusText = "WSL did not report a default distro to open.";
            return Task.CompletedTask;
        }

        return ExecuteActionAsync(
            cancellationToken => distroService.OpenDefaultDistroAsync(cancellationToken),
            $"Opened the default distro {State.DefaultDistroName} in a terminal.",
            refreshAfterSuccess: false);
    }

    public Task OpenDistroAsync(string distroName)
        => ExecuteActionAsync(
            cancellationToken => distroService.OpenDistroAsync(distroName, cancellationToken),
            $"Opened {distroName} in a terminal.",
            refreshAfterSuccess: false);

    public Task StartDistroAsync(string distroName)
        => ExecuteActionAsync(
            cancellationToken => distroService.StartDistroAsync(distroName, cancellationToken),
            $"Started {distroName}.",
            refreshAfterSuccess: true);

    public Task TerminateDistroAsync(string distroName)
        => ExecuteActionAsync(
            cancellationToken => distroService.TerminateDistroAsync(distroName, cancellationToken),
            $"Terminated {distroName}.",
            refreshAfterSuccess: true);

    public Task SetDefaultDistroAsync(string distroName)
        => ExecuteActionAsync(
            cancellationToken => distroService.SetDefaultDistroAsync(distroName, cancellationToken),
            $"Set {distroName} as the default distro.",
            refreshAfterSuccess: true);

    public Task ShutdownAsync()
        => ExecuteActionAsync(
            cancellationToken => distroService.ShutdownAsync(cancellationToken),
            "Shut down all running WSL distros.",
            refreshAfterSuccess: true);

    private async Task ExecuteActionAsync(
        Func<CancellationToken, Task<CommandResult>> action,
        string successMessage,
        bool refreshAfterSuccess)
    {
        if (IsActionInProgress)
        {
            return;
        }

        IsActionInProgress = true;

        try
        {
            // WSL lifecycle operations (terminate, shutdown, set-default) are not safely
            // cancellable mid-flight; partial execution could leave WSL in an inconsistent state.
            var result = await action(CancellationToken.None);
            ActionStatusText = result.IsSuccess
                ? successMessage
                : result.Error?.Summary ?? "The WSL action failed.";

            if ((refreshAfterSuccess && result.IsSuccess) || ShouldRefreshAfter(result))
            {
                await RefreshAsync();
            }
        }
        finally
        {
            IsActionInProgress = false;
        }
    }

    private static bool ShouldRefreshAfter(CommandResult result)
    {
        return result.Error?.Kind is WslErrorKind.AlreadyRunning or WslErrorKind.AlreadyStopped or WslErrorKind.DistroNotFound;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
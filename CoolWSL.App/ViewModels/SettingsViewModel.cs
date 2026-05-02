using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.App.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IDashboardStatusService statusService;
    private readonly IWslDistroService distroService;
    private readonly IWslGlobalConfigService globalConfigService;
    private readonly IAppLogger logger;
    private bool hasLoaded;
    private bool hasDefaultDistro;
    private bool hasGlobalConfigLoaded;
    private bool globalConfigExists;
    private bool globalConfigSaveRequiresRestart;
    private bool isLoading;
    private bool isActionInProgress;
    private string wslStatusText = "Not loaded.";
    private string defaultDistroText = "Not loaded.";
    private string distroSummaryText = "Not loaded.";
    private string lastLoadedText = "Not refreshed yet.";
    private string actionStatusText = string.Empty;
    private string globalConfigPathText = "Not loaded.";
    private string globalConfigStateText = "Not loaded.";
    private string globalConfigContent = string.Empty;
    private string originalGlobalConfigContent = string.Empty;
    private string globalConfigValidationText = "Validation has not run yet.";
    private string globalConfigBackupPathText = "No backup created in this session.";
    private string globalConfigRestartNotice = "Global WSL configuration changes apply after WSL restarts.";
    private WslConfigValidationResult globalConfigValidation = WslConfigValidationResult.Empty;

    public SettingsViewModel(
        IDashboardStatusService statusService,
        IWslDistroService distroService,
        IWslGlobalConfigService globalConfigService,
        IAppLogger logger)
    {
        this.statusService = statusService;
        this.distroService = distroService;
        this.globalConfigService = globalConfigService;
        this.logger = logger;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading
    {
        get => isLoading;
        private set
        {
            if (isLoading == value)
            {
                return;
            }

            isLoading = value;
            OnPropertyChanged();
        }
    }

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
            OnPropertyChanged(nameof(CanRunActions));
            OnPropertyChanged(nameof(CanOpenDefaultDistro));
            OnPropertyChanged(nameof(CanSaveGlobalConfig));
            OnPropertyChanged(nameof(CanRevertGlobalConfig));
            OnPropertyChanged(nameof(CanCreateGlobalConfig));
        }
    }

    public string WslStatusText
    {
        get => wslStatusText;
        private set
        {
            if (string.Equals(wslStatusText, value, StringComparison.Ordinal))
            {
                return;
            }

            wslStatusText = value;
            OnPropertyChanged();
        }
    }

    public string DefaultDistroText
    {
        get => defaultDistroText;
        private set
        {
            if (string.Equals(defaultDistroText, value, StringComparison.Ordinal))
            {
                return;
            }

            defaultDistroText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanOpenDefaultDistro));
        }
    }

    public string DistroSummaryText
    {
        get => distroSummaryText;
        private set
        {
            if (string.Equals(distroSummaryText, value, StringComparison.Ordinal))
            {
                return;
            }

            distroSummaryText = value;
            OnPropertyChanged();
        }
    }

    public string LastLoadedText
    {
        get => lastLoadedText;
        private set
        {
            if (string.Equals(lastLoadedText, value, StringComparison.Ordinal))
            {
                return;
            }

            lastLoadedText = value;
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

    public bool CanRunActions => !IsActionInProgress;

    public bool CanOpenDefaultDistro => CanRunActions && hasDefaultDistro;

    public string GlobalConfigPathText
    {
        get => globalConfigPathText;
        private set
        {
            if (string.Equals(globalConfigPathText, value, StringComparison.Ordinal))
            {
                return;
            }

            globalConfigPathText = value;
            OnPropertyChanged();
        }
    }

    public string GlobalConfigStateText
    {
        get => globalConfigStateText;
        private set
        {
            if (string.Equals(globalConfigStateText, value, StringComparison.Ordinal))
            {
                return;
            }

            globalConfigStateText = value;
            OnPropertyChanged();
        }
    }

    public string GlobalConfigContent
    {
        get => globalConfigContent;
        private set
        {
            if (string.Equals(globalConfigContent, value, StringComparison.Ordinal))
            {
                return;
            }

            globalConfigContent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasGlobalConfigChanges));
            OnPropertyChanged(nameof(HasGlobalConfigRestartNotice));
            OnPropertyChanged(nameof(CanSaveGlobalConfig));
            OnPropertyChanged(nameof(CanRevertGlobalConfig));
            OnPropertyChanged(nameof(CanCreateGlobalConfig));
        }
    }

    public string GlobalConfigValidationText
    {
        get => globalConfigValidationText;
        private set
        {
            if (string.Equals(globalConfigValidationText, value, StringComparison.Ordinal))
            {
                return;
            }

            globalConfigValidationText = value;
            OnPropertyChanged();
        }
    }

    public string GlobalConfigBackupPathText
    {
        get => globalConfigBackupPathText;
        private set
        {
            if (string.Equals(globalConfigBackupPathText, value, StringComparison.Ordinal))
            {
                return;
            }

            globalConfigBackupPathText = value;
            OnPropertyChanged();
        }
    }

    public string GlobalConfigRestartNotice
    {
        get => globalConfigRestartNotice;
        private set
        {
            if (string.Equals(globalConfigRestartNotice, value, StringComparison.Ordinal))
            {
                return;
            }

            globalConfigRestartNotice = value;
            OnPropertyChanged();
        }
    }

    public bool HasGlobalConfigChanges => !string.Equals(GlobalConfigContent, originalGlobalConfigContent, StringComparison.Ordinal);

    public bool HasGlobalConfigRestartNotice => HasGlobalConfigChanges || globalConfigSaveRequiresRestart;

    public bool CanSaveGlobalConfig => hasGlobalConfigLoaded && HasGlobalConfigChanges && !IsActionInProgress && !globalConfigValidation.HasErrors;

    public bool CanRevertGlobalConfig => hasGlobalConfigLoaded && HasGlobalConfigChanges && !IsActionInProgress;

    public bool CanCreateGlobalConfig => hasGlobalConfigLoaded && !globalConfigExists && !HasGlobalConfigChanges && !IsActionInProgress;

    public async Task EnsureLoadedAsync()
    {
        if (hasLoaded || IsLoading)
        {
            return;
        }

        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        IsLoading = true;

        try
        {
            var snapshot = await statusService.GetSnapshotAsync(CancellationToken.None);
            ApplySnapshot(snapshot);
            await LoadGlobalConfigAsync();
            hasLoaded = true;
        }
        catch (Exception ex)
        {
            hasDefaultDistro = false;
            WslStatusText = "WSL status could not be loaded.";
            DefaultDistroText = "No default distro reported.";
            DistroSummaryText = ex.Message;
            LastLoadedText = $"Refresh failed {DateTimeOffset.Now:t}";
            OnPropertyChanged(nameof(CanOpenDefaultDistro));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public Task OpenDefaultDistroAsync()
    {
        if (!CanOpenDefaultDistro)
        {
            ActionStatusText = "WSL did not report a default distro to open.";
            return Task.CompletedTask;
        }

        return ExecuteActionAsync(
            cancellationToken => distroService.OpenDefaultDistroAsync(cancellationToken),
            "Opened the default distro in a terminal.",
            refreshAfterSuccess: false);
    }

    public Task ShutdownAllAsync()
        => ExecuteActionAsync(
            cancellationToken => distroService.ShutdownAsync(cancellationToken),
            "Shut down all running WSL distros.",
            refreshAfterSuccess: true);

    public async Task SaveGlobalConfigAsync()
    {
        if (!CanSaveGlobalConfig)
        {
            return;
        }

        IsActionInProgress = true;

        try
        {
            var result = await globalConfigService.SaveAsync(GlobalConfigContent, CancellationToken.None);
            originalGlobalConfigContent = GlobalConfigContent;
            globalConfigExists = true;
            globalConfigSaveRequiresRestart = true;
            GlobalConfigStateText = $"File saved {result.SavedAt:t}.";
            GlobalConfigBackupPathText = string.IsNullOrWhiteSpace(result.BackupPath)
                ? "No backup was needed because the file did not exist before this save."
                : result.BackupPath;
            GlobalConfigRestartNotice = "Saved .wslconfig changes require WSL to restart. Use Shutdown all WSL only when you are ready.";
            ActionStatusText = "Saved global WSL configuration.";
            logger.LogInfo("Configuration", $"Saved .wslconfig; Backup={(result.BackupPath ?? "none")}");
            ApplyGlobalConfigValidation(result.Validation);
            OnPropertyChanged(nameof(HasGlobalConfigChanges));
            OnPropertyChanged(nameof(HasGlobalConfigRestartNotice));
            OnPropertyChanged(nameof(CanCreateGlobalConfig));
        }
        catch (Exception ex)
        {
            ActionStatusText = $"Could not save .wslconfig: {ex.Message}";
        }
        finally
        {
            IsActionInProgress = false;
        }
    }

    public void RevertGlobalConfig()
    {
        if (!CanRevertGlobalConfig)
        {
            return;
        }

        GlobalConfigContent = originalGlobalConfigContent;
        globalConfigSaveRequiresRestart = false;
        ApplyGlobalConfigValidation(globalConfigService.Validate(GlobalConfigContent));
        ActionStatusText = "Reverted unsaved .wslconfig changes.";
        OnPropertyChanged(nameof(HasGlobalConfigRestartNotice));
    }

    public void CreateGlobalConfig()
    {
        if (!CanCreateGlobalConfig)
        {
            return;
        }

        GlobalConfigContent = "[wsl2]" + Environment.NewLine;
        UpdateGlobalConfigContent(GlobalConfigContent);
        ActionStatusText = "Created an unsaved .wslconfig draft. Review it, then save to write the file.";
    }

    public void UpdateGlobalConfigContent(string content)
    {
        GlobalConfigContent = content;
        ApplyGlobalConfigValidation(globalConfigService.Validate(content));
    }

    private void ApplySnapshot(DashboardStatusSnapshot snapshot)
    {
        var status = snapshot.EnvironmentStatus;
        WslStatusText = status switch
        {
            { Availability: WslAvailability.Available, IsDegraded: true } => $"WSL is degraded: {status.Summary}",
            { Availability: WslAvailability.Available } when !string.IsNullOrWhiteSpace(status.WslVersion) => $"WSL {status.WslVersion}",
            { Availability: WslAvailability.Available } => "WSL is available.",
            { Availability: WslAvailability.NotInstalled } => $"WSL is not installed: {status.Summary}",
            { Availability: WslAvailability.Unavailable } => $"WSL is unavailable: {status.Summary}",
            _ => status.Summary,
        };

        hasDefaultDistro = !string.IsNullOrWhiteSpace(status.DefaultDistroName);
        DefaultDistroText = !hasDefaultDistro
            ? "No default distro reported."
            : status.DefaultDistroName!;

        var runningCount = snapshot.DistroInventory.Distros.Count(static distro => distro.IsRunning);
        DistroSummaryText = $"{snapshot.DistroInventory.Distros.Count} distros installed, {runningCount} running.";
        LastLoadedText = $"Refreshed {DateTimeOffset.Now:t}";
        OnPropertyChanged(nameof(CanOpenDefaultDistro));
    }

    private async Task LoadGlobalConfigAsync()
    {
        var document = await globalConfigService.ReadAsync(CancellationToken.None);
        hasGlobalConfigLoaded = true;
        globalConfigExists = document.Exists;
        globalConfigSaveRequiresRestart = false;
        originalGlobalConfigContent = document.Content;
        GlobalConfigContent = document.Content;
        GlobalConfigPathText = document.Path;
        GlobalConfigStateText = document.Exists
            ? $"Loaded {document.LoadedAt:t}."
            : "File does not exist. WSL is using global defaults.";
        GlobalConfigBackupPathText = "No backup created in this session.";
        GlobalConfigRestartNotice = "Global .wslconfig settings apply only to WSL 2 distributions and take effect after WSL restarts.";
        ApplyGlobalConfigValidation(document.Validation);
        OnPropertyChanged(nameof(CanCreateGlobalConfig));
    }

    private void ApplyGlobalConfigValidation(WslConfigValidationResult validation)
    {
        globalConfigValidation = validation;
        GlobalConfigValidationText = FormatValidation(validation);
        OnPropertyChanged(nameof(CanSaveGlobalConfig));
    }

    private static string FormatValidation(WslConfigValidationResult validation)
    {
        if (validation.Issues.Count == 0)
        {
            return "No syntax or known value issues found.";
        }

        return string.Join(
            Environment.NewLine,
            validation.Issues.Select(static issue =>
            {
                var prefix = issue.Severity switch
                {
                    WslConfigValidationSeverity.Error => "Error",
                    WslConfigValidationSeverity.Warning => "Warning",
                    _ => "Info",
                };
                var location = issue.LineNumber is null ? string.Empty : $" line {issue.LineNumber}:";
                return $"{prefix}{location} {issue.Message}";
            }));
    }

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
            var result = await action(CancellationToken.None);
            ActionStatusText = result.IsSuccess
                ? successMessage
                : result.Error?.Summary ?? "The WSL action failed.";

            if (refreshAfterSuccess && result.IsSuccess)
            {
                await RefreshAsync();
            }
        }
        finally
        {
            IsActionInProgress = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

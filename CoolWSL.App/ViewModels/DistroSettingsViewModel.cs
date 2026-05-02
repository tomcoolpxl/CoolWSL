using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using CoolWSL.App.Helpers;
using CoolWSL.App.Services;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Core.Models.Configuration;
using CoolWSL.Diagnostics.Status;

namespace CoolWSL.App.ViewModels;

public sealed class DistroSettingsViewModel : INotifyPropertyChanged
{
    private readonly IWslDistroConfigService configService;
    private readonly IDashboardStatusService statusService;
    private readonly IWslGlobalConfigService globalConfigService;
    private readonly RefreshCoordinator loadCoordinator = new();

    private string? selectedDistroName;
    private WslDistroConfigDocument? currentDocument;
    
    private bool isLoading;
    private string rawText = string.Empty;
    private bool isModified;
    private string statusMessage = string.Empty;
    private string? backupPath;
    private WslConfigRestartImpact restartImpact = WslConfigRestartImpact.None;
    private string globalWslSummary = string.Empty;

    private WslDistroCapabilityContext? currentCapabilities;
    private bool isSyncing;

    public DistroSettingsViewModel(
        IWslDistroConfigService configService, 
        IDashboardStatusService statusService,
        IWslGlobalConfigService globalConfigService)
    {
        this.configService = configService;
        this.statusService = statusService;
        this.globalConfigService = globalConfigService;
        
        var rows = new List<DistroSettingsRowViewModel>();
        foreach (var key in WslDistroConfigSchema.Current)
        {
            rows.Add(new DistroSettingsRowViewModel(key, UpdateValueFromRow));
        }
        Rows = rows;
    }

    public IReadOnlyList<DistroSettingsRowViewModel> Rows { get; }

    public string RawText
    {
        get => rawText;
        set 
        { 
            if (rawText != value) 
            { 
                rawText = value; 
                OnPropertyChanged();
                IsModified = currentDocument != null && rawText != currentDocument.OriginalContent;
                if (!isSyncing)
                {
                    SyncFromRawText();
                }
            } 
        }
    }

    private void SyncFromRawText()
    {
        if (currentDocument == null || currentCapabilities == null) return;
        isSyncing = true;
        try
        {
            var doc = IniParser.Parse(RawText);
            var validation = configService.Validate(doc, currentCapabilities);
            currentDocument = currentDocument with { Document = doc, Validation = validation };
            foreach (var row in Rows) row.Refresh(doc, currentCapabilities);
            RefreshValidationCollections();
        }
        finally
        {
            isSyncing = false;
        }
    }

    private void UpdateValueFromRow(WslConfigKey schema, string? value)
    {
        if (currentDocument == null || isSyncing) return;
        
        isSyncing = true;
        try
        {
            var doc = IniParser.Parse(RawText);
            var section = doc.Section(schema.Section) ?? new IniSection { Name = schema.Section, RawHeader = $"[{schema.Section}]" };
            
            if (value == null)
            {
                section = section.WithoutEntry(schema.Key);
            }
            else
            {
                var entry = new IniEntry { Key = schema.Key, RawKey = schema.Key, Value = value, OriginalValue = value, RawLine = null };
                section = section.WithEntry(entry);
            }
            
            doc = doc.WithSection(section);
            RawText = doc.Serialize();

            if (currentCapabilities != null)
            {
                var validation = configService.Validate(doc, currentCapabilities);
                currentDocument = currentDocument with { Document = doc, Validation = validation };
                RefreshValidationCollections();
            }
        }
        finally
        {
            isSyncing = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading
    {
        get => isLoading;
        set { if (isLoading != value) { isLoading = value; OnPropertyChanged(); } }
    }

    public bool IsModified
    {
        get => isModified;
        set { if (isModified != value) { isModified = value; OnPropertyChanged(); } }
    }

    public string StatusMessage
    {
        get => statusMessage;
        set { if (statusMessage != value) { statusMessage = value; OnPropertyChanged(); } }
    }

    public string? BackupPath
    {
        get => backupPath;
        set { if (backupPath != value) { backupPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBackupPath)); } }
    }

    public bool HasBackupPath => !string.IsNullOrEmpty(BackupPath);

    public WslConfigRestartImpact RestartImpact
    {
        get => restartImpact;
        set { if (restartImpact != value) { restartImpact = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowRestartPanel)); } }
    }

    public bool ShowRestartPanel => RestartImpact != WslConfigRestartImpact.None;

    public IReadOnlyList<WslConfigValidationIssue> Errors => 
        currentDocument?.Validation.Issues.Where(i => i.Severity == WslConfigValidationSeverity.Error).ToArray() ?? Array.Empty<WslConfigValidationIssue>();

    public IReadOnlyList<WslConfigValidationIssue> Warnings => 
        currentDocument?.Validation.Issues.Where(i => i.Severity == WslConfigValidationSeverity.Warning).ToArray() ?? Array.Empty<WslConfigValidationIssue>();

    public IReadOnlyList<WslConfigValidationIssue> Information => 
        currentDocument?.Validation.Issues.Where(i => i.Severity == WslConfigValidationSeverity.Information).ToArray() ?? Array.Empty<WslConfigValidationIssue>();

    public string GlobalWslSummary
    {
        get => globalWslSummary;
        set { if (globalWslSummary != value) { globalWslSummary = value; OnPropertyChanged(); } }
    }

    public void OpenWslSettings()
    {
        try
        {
            WslSettingsLauncher.Open();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            StatusMessage = $"Could not open the official WSL Settings app: {ex.Message}";
        }
    }

    public void SetSelectedDistro(string? distroName)
    {
        selectedDistroName = distroName;
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        var lease = loadCoordinator.Start();
        var distroName = selectedDistroName;

        if (distroName == null)
        {
            currentDocument = null;
            currentCapabilities = null;
            RawText = string.Empty;
            IsModified = false;
            BackupPath = null;
            RestartImpact = WslConfigRestartImpact.None;
            StatusMessage = "No distro selected.";
            RefreshValidationCollections();
            return;
        }

        IsLoading = true;
        BackupPath = null;
        RestartImpact = WslConfigRestartImpact.None;
        StatusMessage = $"Loading /etc/wsl.conf for {distroName}...";

        try
        {
            var documentTask = configService.ReadAsync(distroName, lease.CancellationToken);
            var settingsContextTask = LoadSettingsContextAsync(distroName, lease.CancellationToken);

            await Task.WhenAll(documentTask, settingsContextTask);

            if (!loadCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            var document = documentTask.Result;
            var settingsContext = settingsContextTask.Result;

            GlobalWslSummary = settingsContext.GlobalSummary;
            ApplyDocument(document, settingsContext.Capabilities);
            IsModified = false;
            StatusMessage = document.Existed 
                ? $"Loaded at {document.LoadedAt:T}." 
                : "File not found - distro is using defaults.";
        }
        catch (OperationCanceledException) when (!loadCoordinator.IsLatest(lease.Version))
        {
        }
        catch (Exception ex)
        {
            if (!loadCoordinator.IsLatest(lease.Version))
            {
                return;
            }

            StatusMessage = $"Load failed: {ex.Message}";
            currentDocument = null;
            currentCapabilities = null;
            RefreshValidationCollections();
        }
        finally
        {
            if (loadCoordinator.IsLatest(lease.Version))
            {
                IsLoading = false;
            }
        }
    }

    public async Task SaveAsync()
    {
        var distroName = selectedDistroName;
        if (distroName == null || !IsModified) return;

        IsLoading = true;
        StatusMessage = "Saving...";

        try
        {
            var settingsContext = await LoadSettingsContextAsync(distroName, CancellationToken.None);
            GlobalWslSummary = settingsContext.GlobalSummary;
            currentCapabilities = settingsContext.Capabilities;

            var newDoc = IniParser.Parse(RawText);
            var validation = configService.Validate(newDoc, currentCapabilities);
            if (validation.HasErrors)
            {
                StatusMessage = "Cannot save: validation blocked by errors.";
                if (currentDocument is not null)
                {
                    currentDocument = currentDocument with { Validation = validation };
                }
                RefreshValidationCollections();
                return;
            }

            var result = await configService.SaveAsync(distroName, newDoc, currentCapabilities, CancellationToken.None);
            var savedDocument = await configService.ReadAsync(distroName, CancellationToken.None);
            ApplyDocument(savedDocument, currentCapabilities);
            IsModified = false;
            BackupPath = result.BackupPath;
            RestartImpact = result.RestartSuggestion;
            
            StatusMessage = result.BackupPath != null 
                ? $"Saved at {result.SavedAt:T}. Backup created."
                : $"Saved at {result.SavedAt:T}. No backup needed.";
                
            RefreshValidationCollections();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<(string GlobalSummary, WslDistroCapabilityContext Capabilities)> LoadSettingsContextAsync(string distroName, CancellationToken cancellationToken)
    {
        var globalConfigTask = globalConfigService.ReadAsync(cancellationToken);
        var snapshotTask = statusService.GetSnapshotAsync(cancellationToken);

        await Task.WhenAll(globalConfigTask, snapshotTask);

        var globalIni = IniParser.Parse(globalConfigTask.Result.Content);
        var memory = globalIni.Section("wsl2")?.Entry("memory")?.EffectiveValue ?? "50% of host";
        var networking = globalIni.Section("wsl2")?.Entry("networkingMode")?.EffectiveValue ?? "NAT";
        var gui = globalIni.Section("wsl2")?.Entry("guiApplications")?.EffectiveValue == "false" ? "off" : "on";
        var globalSummary = globalConfigTask.Result.Exists
            ? $"Global .wslconfig: memory {memory}, networking {networking}, GUI apps {gui}."
            : $"No global .wslconfig file. WSL is using defaults: memory {memory}, networking {networking}, GUI apps {gui}.";

        var snapshot = snapshotTask.Result;
        var distroStatus = snapshot.DistroInventory.Distros.FirstOrDefault(d => d.Name == distroName);
        var capabilities = new WslDistroCapabilityContext(
            Environment.OSVersion.Version.Build,
            snapshot.EnvironmentStatus.WslVersion,
            distroStatus?.WslVersion,
            distroStatus?.IsSystemManaged ?? false,
            Array.Empty<string>());

        return (globalSummary, capabilities);
    }

    private void ApplyDocument(WslDistroConfigDocument document, WslDistroCapabilityContext capabilities)
    {
        currentCapabilities = capabilities;

        var validation = configService.Validate(document.Document, capabilities);
        currentDocument = document with { Validation = validation };

        isSyncing = true;
        try
        {
            RawText = currentDocument.OriginalContent;
        }
        finally
        {
            isSyncing = false;
        }

        foreach (var row in Rows)
        {
            row.Refresh(currentDocument.Document, capabilities);
        }

        RefreshValidationCollections();
    }

    public async Task VerifyAsync()
    {
        if (selectedDistroName == null || currentDocument == null) return;
        IsLoading = true;
        StatusMessage = "Running verification probes...";
        try
        {
            var results = await configService.ProbeAsync(selectedDistroName, currentDocument.Document);
            foreach (var row in Rows)
            {
                var result = results.FirstOrDefault(r => r.KeyId == row.KeyId);
                row.ApplyProbeResult(result);
            }
            StatusMessage = "Verification complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Verification failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Revert()
    {
        if (currentDocument != null)
        {
            RawText = currentDocument.OriginalContent;
            StatusMessage = "Reverted to last loaded state.";
        }
    }

    private void RefreshValidationCollections()
    {
        OnPropertyChanged(nameof(Errors));
        OnPropertyChanged(nameof(Warnings));
        OnPropertyChanged(nameof(Information));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

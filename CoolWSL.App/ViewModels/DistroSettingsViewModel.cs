using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
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
                var entry = new IniEntry { Key = schema.Key, RawKey = schema.Key, Value = value, RawLine = null };
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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "wslsettings:", UseShellExecute = true });
        }
        catch
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "wslsettings.exe", UseShellExecute = true });
            }
            catch
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "ms-settings:", UseShellExecute = true });
            }
        }
    }

    public void SetSelectedDistro(string? distroName)
    {
        selectedDistroName = distroName;
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (selectedDistroName == null)
        {
            currentDocument = null;
            RawText = string.Empty;
            IsModified = false;
            StatusMessage = "No distro selected.";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Loading /etc/wsl.conf for {selectedDistroName}...";

        try
        {
            currentDocument = await configService.ReadAsync(selectedDistroName);
            RawText = currentDocument.OriginalContent;
            IsModified = false;
            StatusMessage = currentDocument.Existed 
                ? $"Loaded at {currentDocument.LoadedAt:T}." 
                : "File not found - distro is using defaults.";
            
            var globalDoc = await globalConfigService.ReadAsync();
            var globalIni = IniParser.Parse(globalDoc.Content);
            var memory = globalIni.Section("wsl2")?.Entry("memory")?.Value ?? "50% of host";
            var networking = globalIni.Section("wsl2")?.Entry("networkingMode")?.Value ?? "NAT";
            var gui = globalIni.Section("wsl2")?.Entry("guiApplications")?.Value == "false" ? "off" : "on";
            GlobalWslSummary = $"Memory: {memory} | Networking: {networking} | GUI apps: {gui}";

            var snap = await statusService.GetSnapshotAsync();
            var distroStatus = snap.DistroInventory.Distros.FirstOrDefault(d => d.Name == selectedDistroName);
            currentCapabilities = new WslDistroCapabilityContext(
                Environment.OSVersion.Version.Build,
                snap.EnvironmentStatus.WslVersion,
                distroStatus?.WslVersion,
                distroStatus?.IsSystemManaged ?? false,
                Array.Empty<string>());
                
            var validation = configService.Validate(currentDocument.Document, currentCapabilities);
            currentDocument = currentDocument with { Validation = validation };
            
            foreach (var row in Rows) row.Refresh(currentDocument.Document, currentCapabilities);
            
            RefreshValidationCollections();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
            currentDocument = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SaveAsync()
    {
        if (selectedDistroName == null || !IsModified) return;

        IsLoading = true;
        StatusMessage = "Saving...";

        try
        {
            var globalDoc = await globalConfigService.ReadAsync();
            var globalIni = IniParser.Parse(globalDoc.Content);
            var memory = globalIni.Section("wsl2")?.Entry("memory")?.Value ?? "50% of host";
            var networking = globalIni.Section("wsl2")?.Entry("networkingMode")?.Value ?? "NAT";
            var gui = globalIni.Section("wsl2")?.Entry("guiApplications")?.Value == "false" ? "off" : "on";
            GlobalWslSummary = $"Memory: {memory} | Networking: {networking} | GUI apps: {gui}";

            var snap = await statusService.GetSnapshotAsync();
            var distroStatus = snap.DistroInventory.Distros.FirstOrDefault(d => d.Name == selectedDistroName);
            currentCapabilities = new WslDistroCapabilityContext(
                Environment.OSVersion.Version.Build,
                snap.EnvironmentStatus.WslVersion,
                distroStatus?.WslVersion,
                distroStatus?.IsSystemManaged ?? false,
                Array.Empty<string>());

            var newDoc = IniParser.Parse(RawText);
            var validation = configService.Validate(newDoc, currentCapabilities);
            if (validation.HasErrors)
            {
                StatusMessage = "Cannot save: validation blocked by errors.";
                currentDocument = currentDocument! with { Validation = validation };
                RefreshValidationCollections();
                return;
            }

            var result = await configService.SaveAsync(selectedDistroName, newDoc, currentCapabilities);
            currentDocument = await configService.ReadAsync(selectedDistroName);
            RawText = currentDocument.OriginalContent;
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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using CoolWSL.Core.Models.Configuration;

namespace CoolWSL.App.ViewModels;

public sealed class DistroSettingsRowViewModel : INotifyPropertyChanged
{
    private readonly WslConfigKey schema;
    private readonly Action<WslConfigKey, string?> updateAction;
    
    private string? currentValue;
    private bool isModified;
    private bool isGated;
    private string? capabilityWarning;

    public DistroSettingsRowViewModel(WslConfigKey schema, Action<WslConfigKey, string?> updateAction)
    {
        this.schema = schema;
        this.updateAction = updateAction;
        KeyId = $"{schema.Section}.{schema.Key}";
        Label = schema.Key; 
        Description = schema.Description;
        IsBoolean = schema.ValueType == WslConfigValueType.Boolean;
        IsString = schema.ValueType != WslConfigValueType.Boolean;
    }

    public string KeyId { get; }
    public string Label { get; }
    public string Description { get; }
    public bool IsBoolean { get; }
    public bool IsString { get; }

    public string? Value
    {
        get => currentValue;
        set
        {
            if (currentValue != value)
            {
                currentValue = value;
                OnPropertyChanged();
                updateAction(schema, value);
            }
        }
    }

    public bool BooleanValue
    {
        get => string.Equals(currentValue, "true", StringComparison.OrdinalIgnoreCase);
        set => Value = value ? "true" : "false";
    }

    public bool IsModified
    {
        get => isModified;
        set { if (isModified != value) { isModified = value; OnPropertyChanged(); } }
    }

    public bool IsGated
    {
        get => isGated;
        private set 
        { 
            if (isGated != value) 
            { 
                isGated = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(IsEditable));
            } 
        }
    }

    public bool IsEditable => !IsGated;

    public string? CapabilityWarning
    {
        get => capabilityWarning;
        private set { if (capabilityWarning != value) { capabilityWarning = value; OnPropertyChanged(); } }
    }

    public void Refresh(IniDocument document, WslDistroCapabilityContext capabilities)
    {
        var entry = document.Section(schema.Section)?.Entry(schema.Key);
        currentValue = entry?.Value;
        IsModified = false;
        
        OnPropertyChanged(nameof(Value));
        if (IsBoolean) OnPropertyChanged(nameof(BooleanValue));
        
        IsGated = false;
        CapabilityWarning = null;
        if (schema.Capability.HasFlag(WslConfigCapabilityRequirement.Windows11Plus) && capabilities.WindowsBuild < 22000)
        {
            IsGated = true;
            CapabilityWarning = "Requires Windows 11.";
        }
        else if (schema.Capability.HasFlag(WslConfigCapabilityRequirement.Wsl2Required) && capabilities.DistroWslVersion == 1)
        {
            IsGated = true;
            CapabilityWarning = "Requires WSL 2.";
        }
        else if (schema.Capability.HasFlag(WslConfigCapabilityRequirement.Systemd067_6Plus) && capabilities.WslVersion == "0.0.0.0") // simplification
        {
            // simplified gating check
        }
    }

    private WslConfigProbeResult? probeResult;

    public WslConfigProbeResult? ProbeResult
    {
        get => probeResult;
        private set 
        { 
            if (probeResult != value) 
            { 
                probeResult = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasProbeResult));
            } 
        }
    }

    public bool HasProbeResult => probeResult != null;

    public void ApplyProbeResult(WslConfigProbeResult? result)
    {
        ProbeResult = result;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

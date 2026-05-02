namespace CoolWSL.Core.Models.Configuration;

[Flags]
public enum WslConfigCapabilityRequirement
{
    None             = 0,
    Wsl2Required     = 1 << 0,
    Windows11Plus    = 1 << 1,   
    Systemd067_6Plus = 1 << 2,   
    NotSystemManaged = 1 << 3,   
}

public enum WslConfigValueType 
{ 
    Boolean, 
    Integer, 
    OctalMask, 
    LinuxPath, 
    Hostname, 
    LinuxUsername, 
    FreeText, 
    DrvFsOptions, 
    Enum 
}

public sealed record WslConfigKey(
    string Section,
    string Key,
    WslConfigValueType ValueType,
    object? Default,
    WslConfigRestartImpact RestartImpact,
    WslConfigCapabilityRequirement Capability,
    string Description,
    string? VerifyCommand,
    bool IsAdvanced,
    string[]? AllowedValues = null);

public static class WslDistroConfigSchema
{
    public static IReadOnlyList<WslConfigKey> Current { get; } = new List<WslConfigKey>
    {
        // [boot]
        new("boot", "systemd", WslConfigValueType.Boolean, null, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.Windows11Plus | WslConfigCapabilityRequirement.Systemd067_6Plus | WslConfigCapabilityRequirement.Wsl2Required, "Enable systemd for this distro.", "test -d /run/systemd/system && readlink /proc/1/exe", false),
        new("boot", "command", WslConfigValueType.FreeText, null, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.Windows11Plus, "Command to run when the WSL instance starts.", null, false),
        new("boot", "protectBinfmt", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.Windows11Plus, "Prevent WSL from generating systemd units when systemd is enabled.", null, false),
        
        // [automount]
        new("automount", "enabled", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Automatically mount fixed Windows drives.", "findmnt -t drvfs -no SOURCE", false),
        new("automount", "mountFsTab", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Process /etc/fstab on WSL startup.", null, false),
        new("automount", "root", WslConfigValueType.LinuxPath, "/mnt/", WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Directory where fixed Windows drives are mounted.", null, false),
        new("automount", "options", WslConfigValueType.DrvFsOptions, null, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "DrvFs options appended to automounted Windows drives.", null, false),

        // [network]
        new("network", "generateHosts", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Generate /etc/hosts on startup.", "test -f /etc/hosts", false),
        new("network", "generateResolvConf", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Generate /etc/resolv.conf on startup.", "ls -l /etc/resolv.conf", false),
        new("network", "hostname", WslConfigValueType.Hostname, null, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Hostname used by this WSL distro.", "hostname", false),

        // [interop]
        new("interop", "enabled", WslConfigValueType.Boolean, true, WslConfigRestartImpact.NewShell, WslConfigCapabilityRequirement.None, "Allow launching Windows executables from Linux.", "command -v powershell.exe", false),
        new("interop", "appendWindowsPath", WslConfigValueType.Boolean, true, WslConfigRestartImpact.NewShell, WslConfigCapabilityRequirement.None, "Append Windows PATH entries to Linux PATH.", null, false),

        // [user]
        new("user", "default", WslConfigValueType.LinuxUsername, null, WslConfigRestartImpact.NewWslSession, WslConfigCapabilityRequirement.None, "Default user when starting this distro.", "id <username>", false),

        // [gpu]
        new("gpu", "enabled", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.Wsl2Required, "Allow Linux applications to access the Windows GPU via paravirtualization.", "test -e /dev/dxg", false),

        // [time]
        new("time", "useWindowsTimezone", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Use and sync to the timezone configured in Windows.", "readlink -f /etc/localtime", false)
    }.AsReadOnly();
}

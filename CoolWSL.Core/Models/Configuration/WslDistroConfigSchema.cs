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
        new("boot", "systemd", WslConfigValueType.Boolean, null, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.Windows11Plus | WslConfigCapabilityRequirement.Systemd067_6Plus | WslConfigCapabilityRequirement.Wsl2Required, "Run systemd as the init process inside this distro. Required for distros and tools that depend on systemctl-managed services. Needs Windows 11 with WSL 0.67.6 or newer.", "test -d /run/systemd/system && readlink /proc/1/exe", false),
        new("boot", "command", WslConfigValueType.FreeText, null, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.Windows11Plus, "Shell command WSL runs as root every time the distro starts. Useful for starting daemons or applying one-off configuration. Output is not surfaced in the terminal so log to a file if you need to inspect it.", null, false),
        new("boot", "protectBinfmt", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.Windows11Plus, "Prevents systemd from clearing the WSL-managed binfmt_misc registration that lets you run Windows .exe files from Linux. Leave this on unless you know you need custom binfmt setup.", null, false),

        // [automount]
        new("automount", "enabled", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Automatically mount fixed Windows drives (C:, D:, ...) inside the distro at startup. Turn off if you want to keep Linux isolated from Windows drives or mount them manually.", "findmnt -t drvfs -no SOURCE", false),
        new("automount", "mountFsTab", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Process /etc/fstab during distro startup so you can declare custom mounts with the standard Linux fstab syntax. Disable if you manage mounts manually or your fstab causes startup hangs.", null, false),
        new("automount", "root", WslConfigValueType.LinuxPath, "/mnt/", WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Directory where automounted Windows drives appear. Default is /mnt/, so C: shows up as /mnt/c. Change to /windows/ or /host/ if you prefer a different layout.", null, false),
        new("automount", "options", WslConfigValueType.DrvFsOptions, null, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "DrvFs options applied to every automounted Windows drive. Common values: metadata (preserve Linux owner/perms), umask=22, fmask=11, case=off. Leave blank for WSL defaults.", null, false),

        // [network]
        new("network", "generateHosts", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Let WSL regenerate /etc/hosts at every startup with entries for the Windows host. Disable if you maintain your own /etc/hosts and don't want it overwritten.", "test -f /etc/hosts", false),
        new("network", "generateResolvConf", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Let WSL regenerate /etc/resolv.conf at every startup using the Windows DNS configuration. Disable if you need custom DNS servers or run your own resolver such as systemd-resolved.", "ls -l /etc/resolv.conf", false),
        new("network", "hostname", WslConfigValueType.Hostname, null, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Hostname WSL applies inside the distro. Defaults to your Windows machine name; set this to give the distro its own identity in shells, prompts, and on the network.", "hostname", false),

        // [interop]
        new("interop", "enabled", WslConfigValueType.Boolean, true, WslConfigRestartImpact.NewShell, WslConfigCapabilityRequirement.None, "Allow Linux to launch Windows executables (powershell.exe, clip.exe, explorer.exe, ...) directly from the shell. Disable for stricter isolation between the two environments.", "command -v powershell.exe", false),
        new("interop", "appendWindowsPath", WslConfigValueType.Boolean, true, WslConfigRestartImpact.NewShell, WslConfigCapabilityRequirement.None, "Append the Windows PATH to the Linux PATH so Windows tools resolve from a Linux shell. Turn off to prevent Windows binaries (for example python.exe) from shadowing Linux versions of the same name.", null, false),

        // [user]
        new("user", "default", WslConfigValueType.LinuxUsername, null, WslConfigRestartImpact.NewWslSession, WslConfigCapabilityRequirement.None, "Username WSL logs in as when this distro starts. Must already exist inside the distro. Useful when you want to default to a non-root account or switch the active user across distros.", "id <username>", false),

        // [gpu]
        new("gpu", "enabled", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.Wsl2Required, "Expose the Windows GPU to Linux through WSL's paravirtualization layer. Required for CUDA, DirectML, OpenGL, and most GPU-accelerated workloads. Disable to fully release the GPU back to Windows.", "test -e /dev/dxg", false),

        // [time]
        new("time", "useWindowsTimezone", WslConfigValueType.Boolean, true, WslConfigRestartImpact.TerminateDistro, WslConfigCapabilityRequirement.None, "Keep the Linux timezone in sync with the Windows timezone. Disable if you want the distro to manage its own /etc/localtime, for example to mirror a production server.", "readlink -f /etc/localtime", false)
    }.AsReadOnly();
}

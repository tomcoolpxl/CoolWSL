using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;

namespace CoolWSL.Configuration.Services;

public sealed class WslGlobalConfigService : IWslGlobalConfigService
{
    private static readonly HashSet<string> KnownSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "wsl2",
        "experimental",
    };

    private static readonly Dictionary<string, HashSet<string>> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["wsl2"] =
        [
            "kernel",
            "kernelModules",
            "memory",
            "processors",
            "localhostForwarding",
            "kernelCommandLine",
            "safeMode",
            "swap",
            "swapFile",
            "guiApplications",
            "debugConsole",
            "maxCrashDumpCount",
            "nestedVirtualization",
            "vmIdleTimeout",
            "dnsProxy",
            "networkingMode",
            "firewall",
            "dnsTunneling",
            "autoProxy",
            "defaultVhdSize",
        ],
        ["experimental"] =
        [
            "autoMemoryReclaim",
            "sparseVhd",
            "bestEffortDnsParsing",
            "dnsTunnelingIpAddress",
            "initialAutoProxyTimeout",
            "ignoredPorts",
            "hostAddressLoopback",
        ],
    };

    private static readonly HashSet<string> BooleanKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhostForwarding",
        "safeMode",
        "guiApplications",
        "debugConsole",
        "nestedVirtualization",
        "dnsProxy",
        "firewall",
        "dnsTunneling",
        "autoProxy",
        "sparseVhd",
        "bestEffortDnsParsing",
        "hostAddressLoopback",
    };

    private static readonly HashSet<string> IntegerKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "processors",
        "maxCrashDumpCount",
        "vmIdleTimeout",
    };

    private static readonly HashSet<string> SizeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "memory",
        "swap",
        "defaultVhdSize",
    };

    private readonly TimeProvider timeProvider;
    private readonly string configPath;
    private readonly string backupDirectory;

    public WslGlobalConfigService(TimeProvider timeProvider)
        : this(timeProvider, GetDefaultConfigPath(), GetDefaultBackupDirectory())
    {
    }

    public WslGlobalConfigService(TimeProvider timeProvider, string configPath, string backupDirectory)
    {
        this.timeProvider = timeProvider;
        this.configPath = configPath;
        this.backupDirectory = backupDirectory;
    }

    public async Task<WslGlobalConfigDocument> ReadAsync(CancellationToken cancellationToken = default)
    {
        var exists = File.Exists(configPath);
        var content = exists
            ? await File.ReadAllTextAsync(configPath, cancellationToken)
            : string.Empty;

        return new WslGlobalConfigDocument(
            configPath,
            exists,
            content,
            timeProvider.GetLocalNow(),
            Validate(content));
    }

    public WslConfigValidationResult Validate(string content)
    {
        var issues = new List<WslConfigValidationIssue>();
        var seenSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenKeysBySection = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        using var reader = new StringReader(content ?? string.Empty);
        var lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
            {
                continue;
            }

            if (trimmed.StartsWith('['))
            {
                ValidateSection(trimmed, lineNumber, issues, seenSections, seenKeysBySection, ref currentSection);
                continue;
            }

            ValidateKeyValue(trimmed, lineNumber, currentSection, issues, seenKeysBySection);
        }

        return issues.Count == 0
            ? WslConfigValidationResult.Empty
            : new WslConfigValidationResult(issues);
    }

    public async Task<WslGlobalConfigSaveResult> SaveAsync(string content, CancellationToken cancellationToken = default)
    {
        var validation = Validate(content);
        if (validation.HasErrors)
        {
            throw new InvalidOperationException("The .wslconfig file contains blocking validation errors.");
        }

        string? backupPath = null;
        if (File.Exists(configPath))
        {
            Directory.CreateDirectory(backupDirectory);
            backupPath = Path.Combine(backupDirectory, $".wslconfig.{timeProvider.GetUtcNow():yyyyMMdd-HHmmss-fffffff}.bak");
            File.Copy(configPath, backupPath, overwrite: false);
        }

        var parentDirectory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        await File.WriteAllTextAsync(configPath, content ?? string.Empty, cancellationToken);

        return new WslGlobalConfigSaveResult(
            configPath,
            backupPath,
            timeProvider.GetLocalNow(),
            validation);
    }

    private static void ValidateSection(
        string trimmed,
        int lineNumber,
        List<WslConfigValidationIssue> issues,
        HashSet<string> seenSections,
        Dictionary<string, HashSet<string>> seenKeysBySection,
        ref string? currentSection)
    {
        if (!trimmed.EndsWith(']') || trimmed.Length <= 2)
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Error,
                "Section headers must use [section] syntax.",
                lineNumber));
            currentSection = null;
            return;
        }

        currentSection = trimmed[1..^1].Trim();
        if (currentSection.Length == 0)
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Error,
                "Section names cannot be empty.",
                lineNumber));
            return;
        }

        if (!seenSections.Add(currentSection))
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Warning,
                $"Section [{currentSection}] appears more than once; WSL may use the later value.",
                lineNumber));
        }

        if (!KnownSections.Contains(currentSection))
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Warning,
                $"Section [{currentSection}] is not a documented .wslconfig section and will be preserved as raw text.",
                lineNumber));
        }

        if (!seenKeysBySection.ContainsKey(currentSection))
        {
            seenKeysBySection[currentSection] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void ValidateKeyValue(
        string trimmed,
        int lineNumber,
        string? currentSection,
        List<WslConfigValidationIssue> issues,
        Dictionary<string, HashSet<string>> seenKeysBySection)
    {
        if (string.IsNullOrWhiteSpace(currentSection))
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Error,
                "Settings must appear under a section such as [wsl2].",
                lineNumber));
            return;
        }

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Error,
                "Settings must use key=value syntax.",
                lineNumber));
            return;
        }

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();
        if (key.Length == 0)
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Error,
                "Setting keys cannot be empty.",
                lineNumber));
            return;
        }

        if (!seenKeysBySection[currentSection].Add(key))
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Warning,
                $"Setting {currentSection}.{key} appears more than once; WSL may use the later value.",
                lineNumber));
        }

        if (KnownKeys.TryGetValue(currentSection, out var knownKeys) && !knownKeys.Contains(key))
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Warning,
                $"Setting {currentSection}.{key} is not documented by WSL and will be preserved as raw text.",
                lineNumber));
        }

        ValidateKnownValue(key, value, lineNumber, issues);
    }

    private static void ValidateKnownValue(
        string key,
        string value,
        int lineNumber,
        List<WslConfigValidationIssue> issues)
    {
        if (BooleanKeys.Contains(key) && !IsBoolean(value))
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Error,
                $"Setting {key} expects true or false.",
                lineNumber));
        }

        if (IntegerKeys.Contains(key) && (!int.TryParse(value, out var number) || number < 0))
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Error,
                $"Setting {key} expects a non-negative number.",
                lineNumber));
        }

        if (SizeKeys.Contains(key) && !IsSize(value))
        {
            issues.Add(new WslConfigValidationIssue(
                WslConfigValidationSeverity.Error,
                $"Setting {key} expects a size such as 8GB, 512MB, or 0.",
                lineNumber));
        }
    }

    private static bool IsBoolean(string value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);

    private static bool IsSize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var numberEnd = 0;
        while (numberEnd < value.Length && char.IsDigit(value[numberEnd]))
        {
            numberEnd++;
        }

        if (numberEnd == 0)
        {
            return false;
        }

        var unit = value[numberEnd..].Trim();
        return unit.Length == 0 ||
               unit.Equals("KB", StringComparison.OrdinalIgnoreCase) ||
               unit.Equals("MB", StringComparison.OrdinalIgnoreCase) ||
               unit.Equals("GB", StringComparison.OrdinalIgnoreCase) ||
               unit.Equals("TB", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDefaultConfigPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wslconfig");

    private static string GetDefaultBackupDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CoolWSL",
            "Backups",
            "WslConfig");
}

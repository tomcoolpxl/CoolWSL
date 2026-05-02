using CoolWSL.Core.Models;
using System.Text.RegularExpressions;

namespace CoolWSL.Core.Models.Configuration;

public static class WslDistroConfigValidator
{
    private static readonly Regex OctalRegex = new("^[0-7]{3,4}$", RegexOptions.Compiled);
    private static readonly Regex HostnameRegex = new("^[a-zA-Z0-9][-a-zA-Z0-9]{0,61}[a-zA-Z0-9]$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new("^[a-z_][a-z0-9_-]*[$]?$", RegexOptions.Compiled);

    public static WslConfigValidationResult Validate(IniDocument document, WslDistroCapabilityContext capabilities)
    {
        var issues = new List<WslConfigValidationIssue>();
        var seenSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in document.Nodes)
        {
            if (node is IniMalformedLine malformed)
            {
                issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Malformed line: {malformed.Reason}", malformed.LineNumber));
                continue;
            }

            if (node is IniSection section)
            {
                if (!seenSections.Add(section.Name))
                {
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Warning, $"Duplicate section [{section.Name}] will override previous entries.", section.LineNumber));
                }

                var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var child in section.Body)
                {
                    if (child is IniMalformedLine cm)
                    {
                        issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Malformed line: {cm.Reason}", cm.LineNumber));
                    }
                    else if (child is IniEntry entry)
                    {
                        if (!seenKeys.Add(entry.Key))
                        {
                            issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Warning, $"Duplicate key '{entry.Key}' will override previous entries.", entry.LineNumber));
                        }

                        ValidateEntry(section.Name, entry, capabilities, issues);
                    }
                }
            }
        }

        return new WslConfigValidationResult(issues);
    }

    private static void ValidateEntry(string sectionName, IniEntry entry, WslDistroCapabilityContext capabilities, List<WslConfigValidationIssue> issues)
    {
        var value = entry.EffectiveValue;
        var schemaKey = WslDistroConfigSchema.Current.FirstOrDefault(k => 
            string.Equals(k.Section, sectionName, StringComparison.OrdinalIgnoreCase) && 
            string.Equals(k.Key, entry.Key, StringComparison.OrdinalIgnoreCase));

        if (schemaKey == null)
        {
            if (WslDistroConfigSchema.Current.Any(k => string.Equals(k.Section, sectionName, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Warning, $"Unknown key '{entry.Key}' in known section [{sectionName}].", entry.LineNumber));
            }
            else
            {
                issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Warning, $"Unknown section [{sectionName}] with key '{entry.Key}'.", entry.LineNumber));
            }
            return;
        }

        entry.IsKnown = true;

        if (schemaKey.Capability.HasFlag(WslConfigCapabilityRequirement.Wsl2Required) && capabilities.DistroWslVersion == 1)
        {
            issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Information, $"Setting requires WSL 2, but this distro is WSL 1. It will be ignored.", entry.LineNumber));
        }
        if (schemaKey.Capability.HasFlag(WslConfigCapabilityRequirement.Windows11Plus) && capabilities.WindowsBuild < 22000)
        {
            issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Information, $"Setting requires Windows 11. It will be ignored.", entry.LineNumber));
        }

        switch (schemaKey.ValueType)
        {
            case WslConfigValueType.Boolean:
                if (!string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) && 
                    !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Value must be 'true' or 'false'.", entry.LineNumber));
                }
                break;
            case WslConfigValueType.Integer:
                if (!uint.TryParse(value, out _))
                {
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Value must be a non-negative integer.", entry.LineNumber));
                }
                break;
            case WslConfigValueType.OctalMask:
                if (!OctalRegex.IsMatch(value))
                {
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Value must be 3 or 4 octal digits.", entry.LineNumber));
                }
                break;
            case WslConfigValueType.LinuxPath:
                if (!value.StartsWith("/"))
                {
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Value must be an absolute Linux path starting with '/'.", entry.LineNumber));
                }
                break;
            case WslConfigValueType.Hostname:
                if (!HostnameRegex.IsMatch(value))
                {
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Value is not a valid hostname.", entry.LineNumber));
                }
                break;
            case WslConfigValueType.LinuxUsername:
                if (!UsernameRegex.IsMatch(value) || value.Length > 32)
                {
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Value is not a valid Linux username.", entry.LineNumber));
                }
                break;
            case WslConfigValueType.Enum:
                if (schemaKey.AllowedValues == null || !schemaKey.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"Value must be one of: {string.Join(", ", schemaKey.AllowedValues ?? Array.Empty<string>())}.", entry.LineNumber));
                }
                break;
            case WslConfigValueType.DrvFsOptions:
                ValidateDrvFsOptions(entry, issues);
                break;
            case WslConfigValueType.FreeText:
                break;
        }
    }

    private static void ValidateDrvFsOptions(IniEntry entry, List<WslConfigValidationIssue> issues)
    {
        var tokens = entry.EffectiveValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            var parts = token.Split('=', 2);
            var key = parts[0];
            var val = parts.Length > 1 ? parts[1] : null;

            switch (key)
            {
                case "metadata":
                    if (val != null) issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"DrvFs option 'metadata' is a flag and should not have a value.", entry.LineNumber));
                    break;
                case "uid":
                case "gid":
                    if (val == null || !uint.TryParse(val, out _)) issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"DrvFs option '{key}' requires an integer value.", entry.LineNumber));
                    break;
                case "umask":
                case "fmask":
                case "dmask":
                    if (val == null || !OctalRegex.IsMatch(val)) issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"DrvFs option '{key}' requires an octal mask value (3-4 digits).", entry.LineNumber));
                    break;
                case "case":
                    if (val == null || (val != "off" && val != "dir" && val != "force")) issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Error, $"DrvFs option 'case' must be 'off', 'dir', or 'force'.", entry.LineNumber));
                    break;
                default:
                    issues.Add(new WslConfigValidationIssue(WslConfigValidationSeverity.Warning, $"Unknown DrvFs option '{key}'.", entry.LineNumber));
                    break;
            }
        }
    }
}

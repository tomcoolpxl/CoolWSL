using CoolWSL.Core.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CoolWSL.Wsl.Parsing;

public sealed class WslListParser
{
    private static readonly Regex ColumnSplitPattern = new(@"\s{2,}", RegexOptions.Compiled);
    private static readonly string[] NoDistributionMarkers =
    {
        "there are no installed distributions",
        "has no installed distributions",
    };

    public WslListParseResult Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return new(Array.Empty<WslDistro>(), true, "WSL returned no distro output to parse.");
        }

        var distros = new List<WslDistro>();
        string? degradedReason = null;

        foreach (var rawLine in output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmedLine = rawLine.Trim();
            if (trimmedLine.Length == 0)
            {
                continue;
            }

            if (NoDistributionMarkers.Any(marker => trimmedLine.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            {
                return new(Array.Empty<WslDistro>(), false, null);
            }

            if (trimmedLine.StartsWith("Windows Subsystem for Linux", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isDefault = false;
            if (trimmedLine.StartsWith('*'))
            {
                isDefault = true;
                trimmedLine = trimmedLine[1..].TrimStart();
            }

            var segments = ColumnSplitPattern.Split(trimmedLine)
                .Where(static segment => segment.Length > 0)
                .ToArray();

            if (segments.Length == 0)
            {
                continue;
            }

            if (segments[0].Equals("NAME", StringComparison.OrdinalIgnoreCase) ||
                (segments.Length >= 3 && segments.All(IsHeaderLikeSegment)))
            {
                continue;
            }

            if (segments.Length < 2)
            {
                degradedReason ??= "The distro list output did not match the documented column layout.";
                continue;
            }

            var name = segments[0].Trim();
            if (name.Length == 0)
            {
                degradedReason ??= "A distro row was missing its name.";
                continue;
            }

            int? version = null;
            string stateLabel;

            if (segments.Length >= 3 && int.TryParse(segments[^1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedVersion))
            {
                version = parsedVersion;
                stateLabel = string.Join(' ', segments.Skip(1).Take(segments.Length - 2));
            }
            else
            {
                stateLabel = string.Join(' ', segments.Skip(1));
                degradedReason ??= "One or more distro rows were missing a WSL version.";
            }

            var state = ParseState(stateLabel);
            if (state == WslDistroState.Unknown)
            {
                degradedReason ??= "One or more distro states were not recognized and were left as unknown.";
            }

            distros.Add(new WslDistro(name, state, stateLabel, version, isDefault));
        }

        if (distros.Count == 0)
        {
            return new(Array.Empty<WslDistro>(), true, degradedReason ?? "The distro list output could not be parsed safely.");
        }

        return new(distros, degradedReason is not null, degradedReason);
    }

    private static bool IsHeaderLikeSegment(string value)
        => value.Any(char.IsLetter) && value.Equals(value.ToUpperInvariant(), StringComparison.Ordinal);

    private static WslDistroState ParseState(string stateLabel)
        => stateLabel.Trim().ToLowerInvariant() switch
        {
            "running" => WslDistroState.Running,
            "stopped" => WslDistroState.Stopped,
            "installing" => WslDistroState.Installing,
            "uninstalling" => WslDistroState.Uninstalling,
            _ => WslDistroState.Unknown,
        };
}

public sealed record WslListParseResult(IReadOnlyList<WslDistro> Distros, bool IsDegraded, string? DegradedReason)
{
    public bool HasNoDistributions => Distros.Count == 0 && !IsDegraded;
}
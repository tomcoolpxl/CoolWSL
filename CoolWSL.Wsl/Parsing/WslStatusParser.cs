using System.Globalization;

namespace CoolWSL.Wsl.Parsing;

public sealed class WslStatusParser
{
    public WslStatusParseResult ParseStatus(string output)
    {
        var fields = ParseFields(output);
        var defaultDistribution = GetValue(fields, "Default Distribution", "Default Distro");
        var defaultVersion = GetInteger(fields, "Default Version");
        var kernelVersion = GetValue(fields, "Kernel version", "Kernel Version");
        var recognizedFieldCount = CountRecognizedValues(defaultDistribution, defaultVersion, kernelVersion);

        return new(
            defaultDistribution,
            defaultVersion,
            kernelVersion,
            fields,
            IsDegraded(output, fields, recognizedFieldCount),
            GetDegradedReason(output, fields, recognizedFieldCount, "status"));
    }

    public WslVersionParseResult ParseVersion(string output)
    {
        var fields = ParseFields(output);
        var wslVersion = GetValue(fields, "WSL version", "Wsl version");
        var kernelVersion = GetValue(fields, "Kernel version", "Kernel Version");
        var windowsVersion = GetValue(fields, "Windows version", "Windows Version");
        var recognizedFieldCount = CountRecognizedValues(wslVersion, kernelVersion, windowsVersion);

        return new(
            wslVersion,
            kernelVersion,
            windowsVersion,
            fields,
            IsDegraded(output, fields, recognizedFieldCount),
            GetDegradedReason(output, fields, recognizedFieldCount, "version"));
    }

    private static Dictionary<string, string> ParseFields(string output)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(output))
        {
            return fields;
        }

        foreach (var rawLine in output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmedLine = rawLine.Trim();
            if (trimmedLine.Length == 0)
            {
                continue;
            }

            var separatorIndex = trimmedLine.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmedLine[..separatorIndex].Trim();
            var value = trimmedLine[(separatorIndex + 1)..].Trim();

            if (key.Length == 0)
            {
                continue;
            }

            fields[key] = value;
        }

        return fields;
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> fields, params string[] candidateKeys)
    {
        foreach (var candidateKey in candidateKeys)
        {
            if (fields.TryGetValue(candidateKey, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static int? GetInteger(IReadOnlyDictionary<string, string> fields, params string[] candidateKeys)
    {
        var value = GetValue(fields, candidateKeys);
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static int CountRecognizedValues(params object?[] values)
        => values.Count(static value => value is not null);

    private static bool IsDegraded(string output, IReadOnlyDictionary<string, string> fields, int recognizedFieldCount)
        => !string.IsNullOrWhiteSpace(output) && (fields.Count == 0 || recognizedFieldCount == 0);

    private static string? GetDegradedReason(string output, IReadOnlyDictionary<string, string> fields, int recognizedFieldCount, string kind)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return $"WSL returned no {kind} details.";
        }

        if (fields.Count == 0)
        {
            return $"The WSL {kind} output did not contain any key-value fields that could be parsed safely.";
        }

        if (recognizedFieldCount == 0)
        {
            return $"The WSL {kind} output format is not one this parser recognizes safely.";
        }

        return null;
    }
}

public sealed record WslStatusParseResult(
    string? DefaultDistribution,
    int? DefaultVersion,
    string? KernelVersion,
    IReadOnlyDictionary<string, string> RawFields,
    bool IsDegraded,
    string? DegradedReason);

public sealed record WslVersionParseResult(
    string? WslVersion,
    string? KernelVersion,
    string? WindowsVersion,
    IReadOnlyDictionary<string, string> RawFields,
    bool IsDegraded,
    string? DegradedReason);
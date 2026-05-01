using CoolWSL.Core.Models;
using System.Globalization;

namespace CoolWSL.Core.Logging;

public static class CommandLogFormatter
{
    public static string Format(CommandResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var exitCodeText = result.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "n/a";
        var errorKindText = result.Error?.Kind.ToString() ?? "None";

        return FormattableString.Invariant(
            $"Command={result.Command.CommandText}; Status={result.Status}; StartedAt={result.StartedAt:O}; EndedAt={result.EndedAt:O}; DurationMs={result.Duration.TotalMilliseconds:0}; ExitCode={exitCodeText}; ErrorKind={errorKindText}");
    }
}
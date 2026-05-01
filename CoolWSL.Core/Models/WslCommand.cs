namespace CoolWSL.Core.Models;

public sealed class WslCommand
{
    public WslCommand(string fileName, IEnumerable<string> arguments, TimeSpan? timeout = null, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        FileName = fileName;
        Arguments = arguments.ToArray();
        Timeout = timeout;
        Description = description;
    }

    public string FileName { get; }

    public IReadOnlyList<string> Arguments { get; }

    public TimeSpan? Timeout { get; }

    public string? Description { get; }

    public string CommandText => string.Join(" ", new[] { QuoteForDisplay(FileName) }.Concat(Arguments.Select(QuoteForDisplay)));

    private static string QuoteForDisplay(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            return "\"\"";
        }

        if (!value.Any(static character => char.IsWhiteSpace(character) || character is '"' or '&' or '|' or '<' or '>' or '^'))
        {
            return value;
        }

        return $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}
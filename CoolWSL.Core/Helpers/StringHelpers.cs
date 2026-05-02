namespace CoolWSL.Core.Helpers;

public static class StringHelpers
{
    public static string CombineDistinct(params string?[] values)
    {
        var parts = new List<string>();

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmedValue = value.Trim();
            if (parts.Contains(trimmedValue, StringComparer.Ordinal))
            {
                continue;
            }

            parts.Add(trimmedValue);
        }

        return string.Join(" ", parts);
    }

    public static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    public static string JoinNonEmpty(string separator, params string?[] values)
        => string.Join(
            separator,
            values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value!.Trim()));
}

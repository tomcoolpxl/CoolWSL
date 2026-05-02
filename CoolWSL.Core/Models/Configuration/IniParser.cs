using System.Text.RegularExpressions;

namespace CoolWSL.Core.Models.Configuration;

public static class IniParser
{
    private static readonly Regex SectionRegex = new(@"^\s*\[(.*?)\]\s*$", RegexOptions.Compiled);
    private static readonly Regex EntryRegex = new(@"^([^=]+?)=(.*)$", RegexOptions.Compiled);

    public static IniDocument Parse(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        var nodes = new List<IniNode>();
        List<IniNode>? currentSectionBody = null;
        IniSection? currentSection = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            
            if (i == lines.Length - 1 && line == "")
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                var blank = new IniBlankLine { LineNumber = i + 1 };
                if (currentSectionBody != null) currentSectionBody.Add(blank);
                else nodes.Add(blank);
                continue;
            }

            char firstChar = line.TrimStart()[0];
            if (firstChar == '#' || firstChar == ';')
            {
                var comment = new IniComment { LineNumber = i + 1, Raw = line };
                if (currentSectionBody != null) currentSectionBody.Add(comment);
                else nodes.Add(comment);
                continue;
            }

            var sectionMatch = SectionRegex.Match(line);
            if (sectionMatch.Success)
            {
                if (currentSection != null)
                {
                    nodes.Add(new IniSection
                    {
                        LineNumber = currentSection.LineNumber,
                        Name = currentSection.Name,
                        RawHeader = currentSection.RawHeader,
                        Body = currentSectionBody!
                    });
                }

                currentSectionBody = new List<IniNode>();
                currentSection = new IniSection
                {
                    LineNumber = i + 1,
                    Name = sectionMatch.Groups[1].Value.Trim().ToLowerInvariant(),
                    RawHeader = line,
                    Body = currentSectionBody
                };
                continue;
            }

            var entryMatch = EntryRegex.Match(line);
            if (entryMatch.Success)
            {
                string rawKey = entryMatch.Groups[1].Value;
                string rawValue = entryMatch.Groups[2].Value;
                string value = rawValue.Trim();
                
                if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                {
                    value = value.Substring(1, value.Length - 2);
                }

                var entry = new IniEntry
                {
                    LineNumber = i + 1,
                    Key = rawKey.Trim().ToLowerInvariant(),
                    RawKey = rawKey,
                    Value = value,
                    RawLine = line
                };

                if (currentSectionBody != null) currentSectionBody.Add(entry);
                else nodes.Add(new IniMalformedLine { LineNumber = i + 1, Raw = line, Reason = "Entry outside of section" });
                continue;
            }

            var malformed = new IniMalformedLine { LineNumber = i + 1, Raw = line, Reason = "Expected key=value or [section]" };
            if (currentSectionBody != null) currentSectionBody.Add(malformed);
            else nodes.Add(malformed);
        }

        if (currentSection != null)
        {
            nodes.Add(new IniSection
            {
                LineNumber = currentSection.LineNumber,
                Name = currentSection.Name,
                RawHeader = currentSection.RawHeader,
                Body = currentSectionBody!
            });
        }

        return new IniDocument(nodes);
    }
}

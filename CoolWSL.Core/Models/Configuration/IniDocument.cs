using System.Text;

namespace CoolWSL.Core.Models.Configuration;

public abstract class IniNode
{
    public int LineNumber { get; init; }
}

public sealed class IniBlankLine : IniNode
{
}

public sealed class IniComment : IniNode
{
    public required string Raw { get; init; }
}

public sealed class IniSection : IniNode
{
    public required string Name { get; init; }
    public required string RawHeader { get; init; }
    public IReadOnlyList<IniNode> Body { get; init; } = Array.Empty<IniNode>();

    public IniEntry? Entry(string key)
    {
        return Body.OfType<IniEntry>().LastOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    public IniSection WithEntry(IniEntry entry)
    {
        var newBody = new List<IniNode>(Body);
        
        var index = newBody.FindLastIndex(n => n is IniEntry e && string.Equals(e.Key, entry.Key, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            newBody[index] = entry;
        }
        else
        {
            int insertIndex = newBody.Count;
            while (insertIndex > 0 && newBody[insertIndex - 1] is IniBlankLine)
            {
                insertIndex--;
            }
            
            if (insertIndex > 0 && newBody[insertIndex - 1] is IniComment)
            {
                newBody.Insert(insertIndex, new IniBlankLine());
                insertIndex++;
            }
            
            newBody.Insert(insertIndex, entry);
        }

        return new IniSection
        {
            LineNumber = LineNumber,
            Name = Name,
            RawHeader = RawHeader,
            Body = newBody
        };
    }

    public IniSection WithoutEntry(string key)
    {
        var newBody = Body.Where(n => !(n is IniEntry e && string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase))).ToList();
        return new IniSection
        {
            LineNumber = LineNumber,
            Name = Name,
            RawHeader = RawHeader,
            Body = newBody
        };
    }
}

public sealed class IniEntry : IniNode
{
    public required string Key { get; init; }
    public required string RawKey { get; init; }
    public required string Value { get; init; }
    public string? RawLine { get; init; }
    public bool IsKnown { get; set; }
}

public sealed class IniMalformedLine : IniNode
{
    public required string Raw { get; init; }
    public required string Reason { get; init; }
}

public sealed class IniDocument
{
    public IReadOnlyList<IniNode> Nodes { get; }
    
    public IReadOnlyList<IniSection> Sections => Nodes.OfType<IniSection>().ToList();

    public IniDocument(IReadOnlyList<IniNode> nodes)
    {
        Nodes = nodes;
    }

    public IniSection? Section(string name)
    {
        return Sections.LastOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IniDocument WithSection(IniSection section)
    {
        var newNodes = new List<IniNode>(Nodes);
        var existingIndex = newNodes.FindLastIndex(n => n is IniSection s && string.Equals(s.Name, section.Name, StringComparison.OrdinalIgnoreCase));
        
        if (existingIndex >= 0)
        {
            newNodes[existingIndex] = section;
        }
        else
        {
            if (newNodes.Count > 0 && !(newNodes[^1] is IniBlankLine))
            {
                newNodes.Add(new IniBlankLine());
            }
            
            newNodes.Add(section);
        }

        return new IniDocument(newNodes);
    }

    public string Serialize()
    {
        var sb = new StringBuilder();
        foreach (var node in Nodes)
        {
            SerializeNode(sb, node);
        }
        return sb.ToString();
    }

    private void SerializeNode(StringBuilder sb, IniNode node)
    {
        switch (node)
        {
            case IniBlankLine:
                sb.Append('\n');
                break;
            case IniComment comment:
                sb.Append(comment.Raw).Append('\n');
                break;
            case IniMalformedLine malformed:
                sb.Append(malformed.Raw).Append('\n');
                break;
            case IniSection section:
                sb.Append(section.RawHeader).Append('\n');
                foreach (var child in section.Body)
                {
                    SerializeNode(sb, child);
                }
                break;
            case IniEntry entry:
                if (string.IsNullOrEmpty(entry.RawLine) || (!entry.RawLine.Contains(entry.Value) && entry.Value.Length > 0))
                {
                    string val = entry.Value;
                    if (val.Contains(", "))
                    {
                        val = $"\"{val}\"";
                    }
                    sb.Append($"{entry.RawKey}={val}\n");
                }
                else
                {
                    sb.Append(entry.RawLine).Append('\n');
                }
                break;
        }
    }
}

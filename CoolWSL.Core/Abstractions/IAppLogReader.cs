using CoolWSL.Core.Models;

namespace CoolWSL.Core.Abstractions;

public interface IAppLogReader
{
    IReadOnlyList<AppLogEntry> GetEntries();
}

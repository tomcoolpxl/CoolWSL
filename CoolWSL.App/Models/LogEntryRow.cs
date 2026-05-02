using CoolWSL.Core.Models;
using System.Globalization;

namespace CoolWSL.App.Models;

public sealed class LogEntryRow
{
    public LogEntryRow(AppLogEntry entry)
    {
        TimestampText = entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        Level = entry.Level;
        Area = entry.Area;
        Message = entry.Message;
    }

    public string TimestampText { get; }

    public string Level { get; }

    public string Area { get; }

    public string Message { get; }
}

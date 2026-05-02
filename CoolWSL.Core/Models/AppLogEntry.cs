namespace CoolWSL.Core.Models;

public sealed record AppLogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Area,
    string Message);

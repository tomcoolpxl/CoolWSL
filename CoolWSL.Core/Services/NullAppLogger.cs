using CoolWSL.Core.Abstractions;

namespace CoolWSL.Core.Services;

public sealed class NullAppLogger : IAppLogger
{
    public void LogInfo(string area, string message)
    {
    }
}
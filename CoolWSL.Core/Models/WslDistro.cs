namespace CoolWSL.Core.Models;

public sealed record WslDistro(string Name, WslDistroState State, string StateLabel, int? WslVersion, bool IsDefault)
{
    public bool IsRunning => State == WslDistroState.Running;

    public bool IsSystemManaged =>
        Name.StartsWith("docker-desktop", StringComparison.OrdinalIgnoreCase);
}
namespace CoolWSL.Core.Models.Configuration;

public sealed record WslDistroCapabilityContext(
    int WindowsBuild,
    string? WslVersion,
    int? DistroWslVersion,
    bool IsSystemManaged,
    IReadOnlyList<string> ExistingUsers);

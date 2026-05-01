using CoolWSL.Core.Models;

namespace CoolWSL.Diagnostics.Status;

public sealed record DashboardStatusSnapshot(WslEnvironmentStatus EnvironmentStatus, WslDistroInventory DistroInventory);
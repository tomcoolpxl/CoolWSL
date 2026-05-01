namespace CoolWSL.Core.Models;

public enum WslErrorKind
{
    NotInstalled,
    Unavailable,
    UnsupportedFeature,
    AccessDenied,
    DistroNotFound,
    AlreadyRunning,
    AlreadyStopped,
    Timeout,
    Cancelled,
    LaunchFailed,
    Unknown,
}
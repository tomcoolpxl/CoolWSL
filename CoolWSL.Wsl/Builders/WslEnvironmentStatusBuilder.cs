using CoolWSL.Core.Models;
using CoolWSL.Wsl.Errors;
using CoolWSL.Wsl.Parsing;

namespace CoolWSL.Wsl.Builders;

public static class WslEnvironmentStatusBuilder
{
    public static WslEnvironmentStatus Build(
        CommandResult statusCommandResult,
        CommandResult versionCommandResult,
        WslStatusParser statusParser,
        WslErrorMapper errorMapper)
    {
        ArgumentNullException.ThrowIfNull(statusCommandResult);
        ArgumentNullException.ThrowIfNull(versionCommandResult);
        ArgumentNullException.ThrowIfNull(statusParser);
        ArgumentNullException.ThrowIfNull(errorMapper);

        if (!statusCommandResult.IsSuccess && statusCommandResult.Error?.Kind is not WslErrorKind.UnsupportedFeature)
        {
            return CreateUnavailableEnvironment(statusCommandResult.Error);
        }

        var statusParseResult = statusCommandResult.IsSuccess
            ? statusParser.ParseStatus(statusCommandResult.StandardOutput)
            : new WslStatusParseResult(null, null, null, new Dictionary<string, string>(), true, statusCommandResult.Error?.Summary);

        var versionParseResult = versionCommandResult.IsSuccess
            ? statusParser.ParseVersion(versionCommandResult.StandardOutput)
            : null;

        var isVersionUnsupported = versionCommandResult.Error?.Kind == WslErrorKind.UnsupportedFeature;
        var isDegraded =
            statusCommandResult.Error?.Kind == WslErrorKind.UnsupportedFeature ||
            statusParseResult.IsDegraded ||
            isVersionUnsupported ||
            versionParseResult?.IsDegraded == true;

        var degradedReason = string.Join(
            " ",
            new[]
            {
                statusCommandResult.Error?.Kind == WslErrorKind.UnsupportedFeature ? statusCommandResult.Error.Summary : null,
                statusParseResult.DegradedReason,
                isVersionUnsupported ? versionCommandResult.Error?.Summary : null,
                versionParseResult?.DegradedReason,
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        var suggestedNextStep = versionCommandResult.Error?.SuggestedNextStep ?? statusCommandResult.Error?.SuggestedNextStep;

        return new(
            WslAvailability.Available,
            isDegraded
                ? "WSL is available, but some environment details could not be determined safely."
                : "WSL is available.",
            statusParseResult.DefaultDistribution,
            statusParseResult.DefaultVersion,
            versionParseResult?.KernelVersion ?? statusParseResult.KernelVersion,
            versionParseResult?.WslVersion,
            versionParseResult?.WindowsVersion,
            isDegraded,
            degradedReason.Length == 0 ? null : degradedReason,
            suggestedNextStep);
    }

    private static WslEnvironmentStatus CreateUnavailableEnvironment(WslCommandError? error)
    {
        if (error?.Kind == WslErrorKind.NotInstalled)
        {
            return new(WslAvailability.NotInstalled, error.Summary, null, null, null, null, null, false, null, error.SuggestedNextStep);
        }

        return new(
            WslAvailability.Unavailable,
            error?.Summary ?? "WSL status could not be loaded.",
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            error?.SuggestedNextStep);
    }
}

using CoolWSL.Core.Models;
using CoolWSL.Wsl.Errors;
using CoolWSL.Wsl.Parsing;

namespace CoolWSL.Wsl.Builders;

public static class WslDistroInventoryBuilder
{
    public static WslDistroInventory Build(CommandResult listResult, CommandResult? runningListResult, WslListParser listParser, WslErrorMapper errorMapper)
    {
        ArgumentNullException.ThrowIfNull(listResult);
        ArgumentNullException.ThrowIfNull(listParser);
        ArgumentNullException.ThrowIfNull(errorMapper);

        if (!listResult.IsSuccess)
        {
            return CreateUnavailableInventory(listResult.Error);
        }

        var parseResult = listParser.Parse(listResult.StandardOutput);
        if (parseResult.Distros.Any(static distro => distro.State == WslDistroState.Unknown) && runningListResult?.IsSuccess == true)
        {
            var runningDistros = listParser.ParseDistroNames(runningListResult.StandardOutput);
            parseResult = listParser.InferStatesFromRunningList(parseResult, runningDistros);
        }

        if (parseResult.HasNoDistributions)
        {
            return new(
                WslAvailability.Available,
                Array.Empty<WslDistro>(),
                "WSL is available, but no Linux distributions are installed.");
        }

        if (parseResult.IsDegraded)
        {
            return new(
                WslAvailability.Available,
                parseResult.Distros,
                "WSL distro inventory is only partially available.",
                true,
                parseResult.DegradedReason,
                "Refresh the inventory after updating WSL if you need full distro details.");
        }

        return new(
            WslAvailability.Available,
            parseResult.Distros,
            "Loaded WSL distro inventory.");
    }

    private static WslDistroInventory CreateUnavailableInventory(WslCommandError? error)
    {
        if (error?.Kind == WslErrorKind.NotInstalled)
        {
            return new(WslAvailability.NotInstalled, Array.Empty<WslDistro>(), error.Summary, false, null, error.SuggestedNextStep);
        }

        if (error?.Kind == WslErrorKind.UnsupportedFeature)
        {
            return new(
                WslAvailability.Available,
                Array.Empty<WslDistro>(),
                "WSL is available, but this installation does not support verbose distro inventory.",
                true,
                error.Summary,
                error.SuggestedNextStep);
        }

        return new(
            WslAvailability.Unavailable,
            Array.Empty<WslDistro>(),
            error?.Summary ?? "WSL inventory could not be loaded.",
            false,
            null,
            error?.SuggestedNextStep);
    }
}

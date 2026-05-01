using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Wsl.Commands;
using CoolWSL.Wsl.Parsing;

namespace CoolWSL.Wsl.Services;

public sealed class WslDistroService : IWslDistroService
{
    private readonly IWslCommandService commandService;
    private readonly WslListParser listParser;
    private readonly WslStatusParser statusParser;

    public WslDistroService(IWslCommandService commandService, WslListParser listParser, WslStatusParser statusParser)
    {
        this.commandService = commandService;
        this.listParser = listParser;
        this.statusParser = statusParser;
    }

    public async Task<WslDistroInventory> GetDistroInventoryAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandService.ExecuteAsync(WslCommandFactory.CreateListVerboseCommand(), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return CreateUnavailableInventory(result.Error);
        }

        var parseResult = listParser.Parse(result.StandardOutput);
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

    public async Task<WslEnvironmentStatus> GetEnvironmentStatusAsync(CancellationToken cancellationToken = default)
    {
        var statusCommandResult = await commandService.ExecuteAsync(WslCommandFactory.CreateStatusCommand(), cancellationToken).ConfigureAwait(false);

        if (!statusCommandResult.IsSuccess && statusCommandResult.Error?.Kind is not WslErrorKind.UnsupportedFeature)
        {
            return CreateUnavailableEnvironment(statusCommandResult.Error);
        }

        var statusParseResult = statusCommandResult.IsSuccess
            ? statusParser.ParseStatus(statusCommandResult.StandardOutput)
            : new WslStatusParseResult(null, null, null, new Dictionary<string, string>(), true, statusCommandResult.Error?.Summary);

        var versionCommandResult = await commandService.ExecuteAsync(WslCommandFactory.CreateVersionCommand(), cancellationToken).ConfigureAwait(false);
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
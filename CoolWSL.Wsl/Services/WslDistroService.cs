using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Logging;
using CoolWSL.Core.Models;
using CoolWSL.Wsl.Commands;
using CoolWSL.Wsl.Errors;
using CoolWSL.Wsl.Parsing;
using System.ComponentModel;
using System.Diagnostics;

namespace CoolWSL.Wsl.Services;

public sealed class WslDistroService : IWslDistroService
{
    private readonly IWslCommandService commandService;
    private readonly WslErrorMapper errorMapper;
    private readonly WslListParser listParser;
    private readonly IAppLogger logger;
    private readonly WslStatusParser statusParser;
    private readonly TimeProvider timeProvider;

    public WslDistroService(
        IWslCommandService commandService,
        WslErrorMapper errorMapper,
        WslListParser listParser,
        WslStatusParser statusParser,
        IAppLogger logger,
        TimeProvider timeProvider)
    {
        this.commandService = commandService;
        this.errorMapper = errorMapper;
        this.listParser = listParser;
        this.logger = logger;
        this.statusParser = statusParser;
        this.timeProvider = timeProvider;
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

    public Task<CommandResult> OpenDefaultDistroAsync(CancellationToken cancellationToken = default)
        => LaunchAsync(WslCommandFactory.CreateOpenDefaultDistroCommand(), cancellationToken);

    public Task<CommandResult> OpenDistroAsync(string distroName, CancellationToken cancellationToken = default)
        => LaunchAsync(WslCommandFactory.CreateOpenDistroCommand(distroName), cancellationToken);

    public Task<CommandResult> StartDistroAsync(string distroName, CancellationToken cancellationToken = default)
        => commandService.ExecuteAsync(WslCommandFactory.CreateStartDistroCommand(distroName), cancellationToken);

    public Task<CommandResult> TerminateDistroAsync(string distroName, CancellationToken cancellationToken = default)
        => commandService.ExecuteAsync(WslCommandFactory.CreateTerminateDistroCommand(distroName), cancellationToken);

    public Task<CommandResult> SetDefaultDistroAsync(string distroName, CancellationToken cancellationToken = default)
        => commandService.ExecuteAsync(WslCommandFactory.CreateSetDefaultDistroCommand(distroName), cancellationToken);

    public Task<CommandResult> ShutdownAsync(CancellationToken cancellationToken = default)
        => commandService.ExecuteAsync(WslCommandFactory.CreateShutdownCommand(), cancellationToken);

    public Task<CommandResult> RunInDistroAsync(string distroName, string commandText, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => commandService.ExecuteAsync(WslCommandFactory.CreateRunInDistroCommand(distroName, commandText, timeout), cancellationToken);

    private Task<CommandResult> LaunchAsync(WslCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = timeProvider.GetUtcNow();
        using var process = new Process { StartInfo = CreateLaunchStartInfo(command) };

        try
        {
            if (!process.Start())
            {
                var failedAt = timeProvider.GetUtcNow();
                return Task.FromResult(CreateLaunchFailure(command, startedAt, failedAt, new InvalidOperationException("The process did not start.")));
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            var failedAt = timeProvider.GetUtcNow();
            return Task.FromResult(CreateLaunchFailure(command, startedAt, failedAt, exception));
        }

        var completedAt = timeProvider.GetUtcNow();
        var result = CommandResult.Launched(command, startedAt, completedAt);
        logger.LogInfo("WSL.Command", CommandLogFormatter.Format(result));
        return Task.FromResult(result);
    }

    private static ProcessStartInfo CreateLaunchStartInfo(WslCommand command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            UseShellExecute = true,
        };

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private CommandResult CreateLaunchFailure(WslCommand command, DateTimeOffset startedAt, DateTimeOffset endedAt, Exception exception)
    {
        var result = CommandResult.LaunchFailed(command, startedAt, endedAt, errorMapper.MapFailure(command, string.Empty, string.Empty, null, exception));
        logger.LogInfo("WSL.Command", CommandLogFormatter.Format(result));
        return result;
    }
}
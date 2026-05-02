using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Logging;
using CoolWSL.Core.Models;
using CoolWSL.Wsl.Builders;
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
        return WslDistroInventoryBuilder.Build(result, listParser, errorMapper);
    }

    public async Task<WslEnvironmentStatus> GetEnvironmentStatusAsync(CancellationToken cancellationToken = default)
    {
        var statusCommandResult = await commandService.ExecuteAsync(WslCommandFactory.CreateStatusCommand(), cancellationToken).ConfigureAwait(false);
        var versionCommandResult = await commandService.ExecuteAsync(WslCommandFactory.CreateVersionCommand(), cancellationToken).ConfigureAwait(false);
        return WslEnvironmentStatusBuilder.Build(statusCommandResult, versionCommandResult, statusParser, errorMapper);
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
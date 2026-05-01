using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Logging;
using CoolWSL.Core.Models;
using CoolWSL.Wsl.Errors;
using System.ComponentModel;
using System.Diagnostics;

namespace CoolWSL.Wsl.Services;

public sealed class WslCommandService : IWslCommandService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private readonly WslErrorMapper errorMapper;
    private readonly IAppLogger logger;
    private readonly TimeProvider timeProvider;

    public WslCommandService(WslErrorMapper errorMapper, IAppLogger logger, TimeProvider timeProvider)
    {
        this.errorMapper = errorMapper;
        this.logger = logger;
        this.timeProvider = timeProvider;
    }

    public async Task<CommandResult> ExecuteAsync(WslCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var startedAt = timeProvider.GetUtcNow();
        using var process = new Process { StartInfo = CreateStartInfo(command) };

        try
        {
            if (!process.Start())
            {
                var endAt = timeProvider.GetUtcNow();
                return CreateLaunchFailure(command, startedAt, endAt, new InvalidOperationException("The process did not start."));
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            var endAt = timeProvider.GetUtcNow();
            return CreateLaunchFailure(command, startedAt, endAt, exception);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var timeoutCts = command.Timeout is TimeSpan timeout
            ? new CancellationTokenSource(timeout)
            : null;
        using var linkedCts = timeoutCts is null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
        {
            return await CompleteInterruptedExecutionAsync(
                process,
                command,
                startedAt,
                CommandExecutionStatus.TimedOut,
                errorMapper.CreateTimeoutError(command.Timeout ?? DefaultTimeout),
                outputTask,
                errorTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await CompleteInterruptedExecutionAsync(
                process,
                command,
                startedAt,
                CommandExecutionStatus.Cancelled,
                errorMapper.CreateCancellationError(),
                outputTask,
                errorTask).ConfigureAwait(false);
        }

        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

        var completedAt = timeProvider.GetUtcNow();
        var standardOutput = await outputTask.ConfigureAwait(false);
        var standardError = await errorTask.ConfigureAwait(false);

        CommandResult result = process.ExitCode == 0
            ? CommandResult.Succeeded(command, startedAt, completedAt, standardOutput, standardError, process.ExitCode)
            : CommandResult.Failed(
                command,
                startedAt,
                completedAt,
                standardOutput,
                standardError,
                process.ExitCode,
                errorMapper.MapFailure(command, standardOutput, standardError, process.ExitCode));

        logger.LogInfo("WSL.Command", CommandLogFormatter.Format(result));
        return result;
    }

    private static ProcessStartInfo CreateStartInfo(WslCommand command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private async Task<CommandResult> CompleteInterruptedExecutionAsync(
        Process process,
        WslCommand command,
        DateTimeOffset startedAt,
        CommandExecutionStatus status,
        WslCommandError error,
        Task<string> outputTask,
        Task<string> errorTask)
    {
        TryKill(process);

        try
        {
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }

        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);

        var completedAt = timeProvider.GetUtcNow();
        var standardOutput = await outputTask.ConfigureAwait(false);
        var standardError = await errorTask.ConfigureAwait(false);
        var exitCode = process.HasExited ? (int?)process.ExitCode : null;

        var result = status switch
        {
            CommandExecutionStatus.TimedOut => CommandResult.TimedOut(command, startedAt, completedAt, standardOutput, standardError, exitCode, error),
            CommandExecutionStatus.Cancelled => CommandResult.Cancelled(command, startedAt, completedAt, standardOutput, standardError, exitCode, error),
            _ => throw new InvalidOperationException($"Unsupported interrupted status {status}.")
        };

        logger.LogInfo("WSL.Command", CommandLogFormatter.Format(result));
        return result;
    }

    private CommandResult CreateLaunchFailure(WslCommand command, DateTimeOffset startedAt, DateTimeOffset endedAt, Exception exception)
    {
        var result = CommandResult.LaunchFailed(command, startedAt, endedAt, errorMapper.MapFailure(command, string.Empty, string.Empty, null, exception));
        logger.LogInfo("WSL.Command", CommandLogFormatter.Format(result));
        return result;
    }

    private static void TryKill(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
    }
}
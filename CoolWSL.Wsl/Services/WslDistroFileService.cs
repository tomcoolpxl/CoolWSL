using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using System.Text;

namespace CoolWSL.Wsl.Services;

public sealed class WslDistroFileService : IWslDistroFileService
{
    private readonly IWslCommandService commandService;

    public WslDistroFileService(IWslCommandService commandService)
    {
        this.commandService = commandService;
    }

    public async Task<DistroFileReadResult> ReadTextAsync(
        string distroName,
        string linuxPath,
        bool readAsRoot = false,
        CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "-d", distroName };
        if (readAsRoot)
        {
            args.Add("-u");
            args.Add("root");
        }
        args.Add("--exec");
        args.Add("cat");
        args.Add("--");
        args.Add(linuxPath);

        var command = new WslCommand(
            "wsl.exe",
            args.ToArray(),
            standardOutputEncoding: Encoding.UTF8);

        var result = await commandService.ExecuteAsync(command, cancellationToken);
        if (result.IsSuccess)
        {
            return new DistroFileReadResult(result.StandardOutput, true, null);
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError) && result.ExitCode != 0)
        {
            return new DistroFileReadResult(string.Empty, false, result.Error);
        }

        return new DistroFileReadResult(string.Empty, false, result.Error);
    }

    public async Task<DistroFileWriteResult> WriteTextAsync(
        string distroName,
        string linuxPath,
        string contents,
        bool writeAsRoot = true,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var tmpPath = $"/tmp/coolwsl-{id}.tmp";
        
        var shellCmd = $"umask 022 && cat > {tmpPath} && install -m 0644 -o root -g root {tmpPath} {linuxPath} && rm -f {tmpPath}";

        var args = new List<string> { "-d", distroName };
        if (writeAsRoot)
        {
            args.Add("-u");
            args.Add("root");
        }
        args.Add("--exec");
        args.Add("/bin/sh");
        args.Add("-lc");
        args.Add(shellCmd);

        var command = new WslCommand("wsl.exe", args.ToArray());

        var result = await commandService.ExecuteWithStdinAsync(command, contents, cancellationToken);
        
        return new DistroFileWriteResult(result.IsSuccess, result.Error);
    }

    public async Task<bool> ExistsAsync(string distroName, string linuxPath, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "-d", distroName, "--exec", "test", "-e", linuxPath };
        var command = new WslCommand("wsl.exe", args.ToArray());
        var result = await commandService.ExecuteAsync(command, cancellationToken);
        return result.IsSuccess;
    }
}

using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Mappers;
using CoolWSL.Diagnostics.Models;
using System.Text;

namespace CoolWSL.Diagnostics.Services;

public sealed class DiagnosticsService : IDiagnosticsService
{
    private const string DnsProbeCommand = "if command -v getent >/dev/null 2>&1; then getent hosts learn.microsoft.com; elif command -v nslookup >/dev/null 2>&1; then nslookup learn.microsoft.com; elif command -v ping >/dev/null 2>&1; then ping -c 1 learn.microsoft.com; else echo 'No supported DNS test tool was found.' >&2; exit 127; fi";
    private const string InternetProbeCommand = "if command -v curl >/dev/null 2>&1; then curl -I -sS --max-time 10 https://learn.microsoft.com >/dev/null; elif command -v wget >/dev/null 2>&1; then wget -q --spider --timeout=10 https://learn.microsoft.com; elif command -v ping >/dev/null 2>&1; then ping -c 1 1.1.1.1; else echo 'No supported internet test tool was found.' >&2; exit 127; fi";
    private static readonly TimeSpan DiagnosticTimeout = TimeSpan.FromSeconds(20);
    private static readonly Encoding HostWslEncoding = Encoding.Unicode;

    private readonly IWslCommandService commandService;
    private readonly IWslDistroService distroService;
    private readonly DiagnosticSummaryMapper summaryMapper;

    public DiagnosticsService(IWslCommandService commandService, IWslDistroService distroService, DiagnosticSummaryMapper summaryMapper)
    {
        this.commandService = commandService;
        this.distroService = distroService;
        this.summaryMapper = summaryMapper;
    }

    public async Task<DiagnosticsSnapshot> GetSnapshotAsync(string? selectedDistroName, CancellationToken cancellationToken = default)
    {
        var environmentTask = distroService.GetEnvironmentStatusAsync(cancellationToken);
        var inventoryTask = distroService.GetDistroInventoryAsync(cancellationToken);
        var statusCommandTask = commandService.ExecuteAsync(CreateStatusCommand(), cancellationToken);
        var versionCommandTask = commandService.ExecuteAsync(CreateVersionCommand(), cancellationToken);
        var listCommandTask = commandService.ExecuteAsync(CreateInventoryCommand(), cancellationToken);

        await Task.WhenAll(environmentTask, inventoryTask, statusCommandTask, versionCommandTask, listCommandTask).ConfigureAwait(false);

        var environmentStatus = environmentTask.Result;
        var distroInventory = inventoryTask.Result;
        var resolvedDistroName = ResolveSelectedDistroName(selectedDistroName, environmentStatus.DefaultDistroName, distroInventory.Distros);
        var selectedDistro = distroInventory.Distros.FirstOrDefault(distro => string.Equals(distro.Name, resolvedDistroName, StringComparison.Ordinal));

        CommandResult? dnsResult = null;
        CommandResult? internetResult = null;

        if (!string.IsNullOrWhiteSpace(resolvedDistroName))
        {
            var dnsTask = distroService.RunInDistroAsync(resolvedDistroName, DnsProbeCommand, DiagnosticTimeout, cancellationToken);
            var internetTask = distroService.RunInDistroAsync(resolvedDistroName, InternetProbeCommand, DiagnosticTimeout, cancellationToken);
            await Task.WhenAll(dnsTask, internetTask).ConfigureAwait(false);
            dnsResult = dnsTask.Result;
            internetResult = internetTask.Result;
        }

        var results = new List<DiagnosticResult>
        {
            summaryMapper.CreateStatusResult(statusCommandTask.Result, environmentStatus),
            summaryMapper.CreateVersionResult(versionCommandTask.Result, environmentStatus),
            summaryMapper.CreateInventoryResult(listCommandTask.Result, distroInventory),
            summaryMapper.CreateDefaultDistroResult(environmentStatus, distroInventory),
        };

        if (!string.IsNullOrWhiteSpace(resolvedDistroName) && dnsResult is not null && internetResult is not null)
        {
            results.Add(summaryMapper.CreateDnsResult(resolvedDistroName, dnsResult));
            results.Add(summaryMapper.CreateInternetResult(resolvedDistroName, internetResult));
        }

        results.Add(summaryMapper.CreateHostNote(resolvedDistroName, selectedDistro));

        return new(environmentStatus, distroInventory, resolvedDistroName, results);
    }

    private static string? ResolveSelectedDistroName(string? requestedDistroName, string? defaultDistroName, IReadOnlyList<WslDistro> distros)
    {
        if (!string.IsNullOrWhiteSpace(requestedDistroName) && distros.Any(distro => string.Equals(distro.Name, requestedDistroName, StringComparison.Ordinal)))
        {
            return requestedDistroName;
        }

        if (!string.IsNullOrWhiteSpace(defaultDistroName) && distros.Any(distro => string.Equals(distro.Name, defaultDistroName, StringComparison.Ordinal)))
        {
            return defaultDistroName;
        }

        return distros.FirstOrDefault()?.Name;
    }

    private static WslCommand CreateStatusCommand()
        => new("wsl.exe", new[] { "--status" }, TimeSpan.FromSeconds(10), "Read WSL status for diagnostics", HostWslEncoding, HostWslEncoding);

    private static WslCommand CreateVersionCommand()
        => new("wsl.exe", new[] { "--version" }, TimeSpan.FromSeconds(10), "Read WSL version for diagnostics", HostWslEncoding, HostWslEncoding);

    private static WslCommand CreateInventoryCommand()
        => new("wsl.exe", new[] { "--list", "--verbose" }, TimeSpan.FromSeconds(10), "Read distro inventory for diagnostics", HostWslEncoding, HostWslEncoding);
}
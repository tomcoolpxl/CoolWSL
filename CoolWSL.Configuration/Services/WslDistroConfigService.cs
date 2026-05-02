using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Core.Models.Configuration;

namespace CoolWSL.Configuration.Services;

public sealed class WslDistroConfigService : IWslDistroConfigService
{
    private readonly IWslDistroFileService fileService;
    private readonly IWslDistroService distroService;
    private readonly string backupRoot;

    public WslDistroConfigService(IWslDistroFileService fileService, IWslDistroService distroService)
    {
        this.fileService = fileService;
        this.distroService = distroService;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        backupRoot = Path.Combine(localAppData, "CoolWSL", "Backups", "WslDistroConfig");
    }

    public async Task<WslDistroConfigDocument> ReadAsync(string distroName, CancellationToken cancellationToken = default)
    {
        var result = await fileService.ReadTextAsync(distroName, "/etc/wsl.conf", readAsRoot: false, cancellationToken);
        var document = IniParser.Parse(result.Content);
        return new WslDistroConfigDocument(
            distroName,
            document,
            result.Content,
            result.Exists,
            DateTimeOffset.Now,
            WslConfigValidationResult.Empty);
    }

    public WslConfigValidationResult Validate(IniDocument document, WslDistroCapabilityContext capabilities)
    {
        return WslDistroConfigValidator.Validate(document, capabilities);
    }

    public async Task<WslDistroConfigSaveResult> SaveAsync(
        string distroName,
        IniDocument document,
        WslDistroCapabilityContext capabilities,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(document, capabilities);
        if (validation.HasErrors)
        {
            throw new InvalidOperationException("Cannot save configuration with blocking errors.");
        }

        var currentResult = await fileService.ReadTextAsync(distroName, "/etc/wsl.conf", readAsRoot: true, cancellationToken);
        string? backupPath = null;
        
        if (currentResult.Exists)
        {
            var distroBackupDir = Path.Combine(backupRoot, distroName);
            Directory.CreateDirectory(distroBackupDir);
            
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ");
            backupPath = Path.Combine(distroBackupDir, $"wsl.conf.{timestamp}.bak");
            await File.WriteAllTextAsync(backupPath, currentResult.Content, cancellationToken);
        }

        var content = document.Serialize();
        var writeResult = await fileService.WriteTextAsync(distroName, "/etc/wsl.conf", content, writeAsRoot: true, cancellationToken);
        
        if (!writeResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to save /etc/wsl.conf: {writeResult.Error?.Summary}");
        }

        return new WslDistroConfigSaveResult(
            distroName,
            "/etc/wsl.conf",
            backupPath,
            DateTimeOffset.Now,
            validation,
            WslConfigRestartImpact.TerminateDistro);
    }

    public async Task<IReadOnlyList<WslConfigProbeResult>> ProbeAsync(string distroName, IniDocument document, CancellationToken cancellationToken = default)
    {
        var results = new List<WslConfigProbeResult>();
        
        foreach (var schemaKey in WslDistroConfigSchema.Current)
        {
            if (string.IsNullOrEmpty(schemaKey.VerifyCommand)) continue;
            
            var entry = document.Section(schemaKey.Section)?.Entry(schemaKey.Key);
            if (entry == null) continue; 

            var command = schemaKey.VerifyCommand;
            if (command.Contains("<username>"))
            {
                command = command.Replace("<username>", entry.EffectiveValue);
            }

            var result = await distroService.RunInDistroAsync(distroName, command, TimeSpan.FromSeconds(10), cancellationToken);
            
            WslConfigProbeStatus status;
            string evidence;
            if (result.IsSuccess)
            {
                status = WslConfigProbeStatus.Effective;
                evidence = result.StandardOutput;
            }
            else
            {
                status = WslConfigProbeStatus.NotEffective;
                evidence = result.StandardError;
            }

            if (string.IsNullOrWhiteSpace(evidence)) evidence = "(no output)";
            if (evidence.Length > 500) evidence = evidence.Substring(0, 500);

            results.Add(new WslConfigProbeResult(
                $"{schemaKey.Section}.{schemaKey.Key}-probe",
                $"{schemaKey.Section}.{schemaKey.Key}",
                status,
                evidence,
                command,
                DateTimeOffset.Now));
        }
        
        return results;
    }
}

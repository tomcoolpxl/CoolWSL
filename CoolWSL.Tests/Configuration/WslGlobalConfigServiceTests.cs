using CoolWSL.Configuration.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Configuration;

[TestClass]
public sealed class WslGlobalConfigServiceTests
{
    private string? tempRoot;

    [TestCleanup]
    public void Cleanup()
    {
        if (tempRoot is not null && Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void Validate_AcceptsDocumentedGlobalConfig()
    {
        var service = CreateService(out _, out _);

        var result = service.Validate(
            """
            # Settings apply to WSL 2 distros.
            [wsl2]
            memory=8GB
            processors=4
            localhostForwarding=true

            [experimental]
            sparseVhd=false
            """);

        Assert.IsFalse(result.HasErrors);
        Assert.AreEqual(0, result.Issues.Count);
    }

    [TestMethod]
    public void Validate_ReportsMalformedSyntaxAsBlockingError()
    {
        var service = CreateService(out _, out _);

        var result = service.Validate(
            """
            [wsl2
            memory=8GB
            processors
            """);

        Assert.IsTrue(result.HasErrors);
        StringAssert.Contains(string.Join('\n', result.Issues.Select(static issue => issue.Message)), "Section headers");
    }

    [TestMethod]
    public void Validate_PreservesUnknownKeysAsWarnings()
    {
        var service = CreateService(out _, out _);

        var result = service.Validate(
            """
            [wsl2]
            futureSetting=enabled
            """);

        Assert.IsFalse(result.HasErrors);
        Assert.IsTrue(result.HasWarnings);
        StringAssert.Contains(result.Issues.Single().Message, "will be preserved");
    }

    [TestMethod]
    public async Task ReadAsync_WhenMissing_ReturnsEmptyDefaultDocument()
    {
        var service = CreateService(out var configPath, out _);

        var document = await service.ReadAsync();

        Assert.AreEqual(configPath, document.Path);
        Assert.IsFalse(document.Exists);
        Assert.AreEqual(string.Empty, document.Content);
        Assert.IsFalse(document.Validation.HasErrors);
    }

    [TestMethod]
    public async Task SaveAsync_WritesRawContentWithoutNormalizing()
    {
        var service = CreateService(out var configPath, out _);
        var content =
            """
            # Keep this comment.
            [wsl2]
            memory=4GB

            [unknown]
            custom=true
            """;

        await service.SaveAsync(content);

        Assert.AreEqual(content, await File.ReadAllTextAsync(configPath));
    }

    [TestMethod]
    public async Task SaveAsync_CreatesBackupBeforeOverwrite()
    {
        var service = CreateService(out var configPath, out var backupDirectory);
        await File.WriteAllTextAsync(configPath, "[wsl2]\nmemory=2GB\n");

        var result = await service.SaveAsync("[wsl2]\nmemory=4GB\n");

        Assert.IsNotNull(result.BackupPath);
        Assert.IsTrue(File.Exists(result.BackupPath));
        Assert.IsTrue(result.BackupPath.StartsWith(backupDirectory, StringComparison.Ordinal));
        Assert.AreEqual("[wsl2]\nmemory=2GB\n", await File.ReadAllTextAsync(result.BackupPath));
        Assert.AreEqual("[wsl2]\nmemory=4GB\n", await File.ReadAllTextAsync(configPath));
    }

    [TestMethod]
    public async Task SaveAsync_DoesNotWriteMalformedConfig()
    {
        var service = CreateService(out var configPath, out _);
        await File.WriteAllTextAsync(configPath, "[wsl2]\nmemory=2GB\n");

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.SaveAsync("[wsl2]\nmemory=nope\n"));

        Assert.AreEqual("[wsl2]\nmemory=2GB\n", await File.ReadAllTextAsync(configPath));
    }

    private WslGlobalConfigService CreateService(out string configPath, out string backupDirectory)
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "CoolWSL.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        configPath = Path.Combine(tempRoot, ".wslconfig");
        backupDirectory = Path.Combine(tempRoot, "Backups");

        return new WslGlobalConfigService(new FixedTimeProvider(), configPath, backupDirectory);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => new(2026, 5, 2, 12, 34, 56, TimeSpan.Zero);
    }
}

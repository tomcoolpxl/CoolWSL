using CoolWSL.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Core;

[TestClass]
public sealed class FileAppLoggerTests
{
    private string? logDirectory;

    [TestCleanup]
    public void Cleanup()
    {
        if (logDirectory is not null && Directory.Exists(logDirectory))
        {
            Directory.Delete(logDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void LogInfo_StoresMetadataEntry()
    {
        var logger = CreateLogger();

        logger.LogInfo("WSL.Command", "Command=wsl --status; Status=Succeeded");

        var entry = logger.GetEntries().Single();
        Assert.AreEqual("Info", entry.Level);
        Assert.AreEqual("WSL.Command", entry.Area);
        StringAssert.Contains(entry.Message, "Status=Succeeded");
    }

    [TestMethod]
    public void GetEntries_ReturnsNewestFirst()
    {
        var timeProvider = new IncrementingTimeProvider();
        var logger = CreateLogger(timeProvider);

        logger.LogInfo("First", "Older");
        logger.LogInfo("Second", "Newer");

        var entries = logger.GetEntries();
        Assert.AreEqual("Second", entries[0].Area);
        Assert.AreEqual("First", entries[1].Area);
    }

    private FileAppLogger CreateLogger(TimeProvider? timeProvider = null)
    {
        logDirectory = Path.Combine(Path.GetTempPath(), "CoolWSL.Tests", Guid.NewGuid().ToString("N"));
        return new FileAppLogger(timeProvider ?? TimeProvider.System, logDirectory);
    }

    private sealed class IncrementingTimeProvider : TimeProvider
    {
        private DateTimeOffset current = new(2026, 5, 2, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            current = current.AddSeconds(1);
            return current;
        }
    }
}

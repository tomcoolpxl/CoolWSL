using CoolWSL.Configuration.Services;
using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Core.Models.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CoolWSL.Tests.Configuration;

[TestClass]
public class WslDistroConfigServiceTests
{
    [TestMethod]
    public async Task ReadAsync_MissingFile_ReturnsEmptyDocumentWithExistedFalse()
    {
        var mockFileService = new Mock<IWslDistroFileService>();
        mockFileService.Setup(f => f.ReadTextAsync("Ubuntu", "/etc/wsl.conf", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistroFileReadResult("", false, null));

        var mockDistroService = new Mock<IWslDistroService>();
        var service = new WslDistroConfigService(mockFileService.Object, mockDistroService.Object);
        var result = await service.ReadAsync("Ubuntu");

        Assert.IsFalse(result.Existed);
        Assert.AreEqual(0, result.Document.Sections.Count);
    }

    [TestMethod]
    public async Task SaveAsync_NewFile_WritesNoBackupAndReturnsSuccess()
    {
        var mockFileService = new Mock<IWslDistroFileService>();
        mockFileService.Setup(f => f.ReadTextAsync("Ubuntu", "/etc/wsl.conf", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistroFileReadResult("", false, null));
        mockFileService.Setup(f => f.WriteTextAsync("Ubuntu", "/etc/wsl.conf", It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistroFileWriteResult(true, null));

        var mockDistroService = new Mock<IWslDistroService>();
        var service = new WslDistroConfigService(mockFileService.Object, mockDistroService.Object);
        var doc = IniParser.Parse("[boot]\nsystemd=true\n");
        var capabilities = new WslDistroCapabilityContext(26100, "2.0.0", 2, false, Array.Empty<string>());

        var result = await service.SaveAsync("Ubuntu", doc, capabilities);

        Assert.IsNull(result.BackupPath);
        Assert.AreEqual("Ubuntu", result.DistroName);
        mockFileService.Verify(f => f.WriteTextAsync("Ubuntu", "/etc/wsl.conf", "[boot]\nsystemd=true\n", true, It.IsAny<CancellationToken>()), Times.Once);
    }
}

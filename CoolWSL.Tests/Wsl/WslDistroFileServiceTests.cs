using CoolWSL.Core.Abstractions;
using CoolWSL.Core.Models;
using CoolWSL.Wsl.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text;

namespace CoolWSL.Tests.Wsl;

[TestClass]
public class WslDistroFileServiceTests
{
    [TestMethod]
    public async Task ReadTextAsync_Success_ReturnsContent()
    {
        var mockCommandService = new Mock<IWslCommandService>();
        mockCommandService.Setup(s => s.ExecuteAsync(It.IsAny<WslCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded(new WslCommand("wsl.exe", Array.Empty<string>()), DateTimeOffset.Now, DateTimeOffset.Now, "hello", "", 0));

        var service = new WslDistroFileService(mockCommandService.Object);
        var result = await service.ReadTextAsync("Ubuntu", "/etc/wsl.conf");

        Assert.IsTrue(result.Exists);
        Assert.AreEqual("hello", result.Content);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public async Task WriteTextAsync_Success_UsesTee()
    {
        var mockCommandService = new Mock<IWslCommandService>();
        mockCommandService.Setup(s => s.ExecuteWithStdinAsync(It.IsAny<WslCommand>(), "newcontent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Succeeded(new WslCommand("wsl.exe", Array.Empty<string>()), DateTimeOffset.Now, DateTimeOffset.Now, "", "", 0));

        var service = new WslDistroFileService(mockCommandService.Object);
        var result = await service.WriteTextAsync("Ubuntu", "/etc/wsl.conf", "newcontent");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Error);
    }
}

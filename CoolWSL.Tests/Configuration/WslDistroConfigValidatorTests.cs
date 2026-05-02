using CoolWSL.Core.Models;
using CoolWSL.Core.Models.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Configuration;

[TestClass]
public class WslDistroConfigValidatorTests
{
    private static WslDistroCapabilityContext CreateContext(int build = 26100, int wslVersion = 2)
        => new(build, "2.0.0", wslVersion, false, Array.Empty<string>());

    [TestMethod]
    public void ValidMinimalBootSystemd_ZeroIssues()
    {
        var doc = IniParser.Parse("[boot]\nsystemd=true\n");
        var result = WslDistroConfigValidator.Validate(doc, CreateContext());
        
        Assert.AreEqual(0, result.Issues.Count, "Issues: " + string.Join(", ", result.Issues.Select(i => i.Message)));
    }

    [TestMethod]
    public void BootSystemdOnWin10_ReturnsCapabilityInfo()
    {
        var doc = IniParser.Parse("[boot]\nsystemd=true\n");
        var result = WslDistroConfigValidator.Validate(doc, CreateContext(build: 19045));
        
        Assert.AreEqual(1, result.Issues.Count);
        Assert.AreEqual(WslConfigValidationSeverity.Information, result.Issues[0].Severity);
    }

    [TestMethod]
    public void BootSystemdInvalidType_ReturnsBlockingError()
    {
        var doc = IniParser.Parse("[boot]\nsystemd=maybe\n");
        var result = WslDistroConfigValidator.Validate(doc, CreateContext());
        
        Assert.AreEqual(1, result.Issues.Count);
        Assert.AreEqual(WslConfigValidationSeverity.Error, result.Issues[0].Severity);
    }

    [TestMethod]
    public void InvalidDrvFsUid_ReturnsError()
    {
        var doc = IniParser.Parse("[automount]\noptions=metadata,uid=abc\n");
        var result = WslDistroConfigValidator.Validate(doc, CreateContext());
        
        Assert.AreEqual(1, result.Issues.Count);
        Assert.AreEqual(WslConfigValidationSeverity.Error, result.Issues[0].Severity);
    }

    [TestMethod]
    public void UnknownDrvFsToken_ReturnsWarning()
    {
        var doc = IniParser.Parse("[automount]\noptions=metadata,unknownThing\n");
        var result = WslDistroConfigValidator.Validate(doc, CreateContext());
        
        Assert.AreEqual(1, result.Issues.Count);
        Assert.AreEqual(WslConfigValidationSeverity.Warning, result.Issues[0].Severity);
    }

    [TestMethod]
    public void UnknownSection_ReturnsWarning()
    {
        var doc = IniParser.Parse("[fileServer]\nenabled=true\n");
        var result = WslDistroConfigValidator.Validate(doc, CreateContext());
        
        Assert.AreEqual(1, result.Issues.Count);
        Assert.AreEqual(WslConfigValidationSeverity.Warning, result.Issues[0].Severity);
    }

    [TestMethod]
    public void DuplicateSection_ReturnsWarning()
    {
        var doc = IniParser.Parse("[boot]\nsystemd=true\n[boot]\ncommand=test\n");
        var result = WslDistroConfigValidator.Validate(doc, CreateContext());
        
        Assert.AreEqual(1, result.Issues.Count);
        Assert.AreEqual(WslConfigValidationSeverity.Warning, result.Issues[0].Severity);
        Assert.IsTrue(result.Issues[0].Message.Contains("Duplicate section"));
    }

    [TestMethod]
    public void DuplicateKey_ReturnsWarning()
    {
        var doc = IniParser.Parse("[boot]\nsystemd=true\nsystemd=false\n");
        var result = WslDistroConfigValidator.Validate(doc, CreateContext());
        
        Assert.AreEqual(1, result.Issues.Count);
        Assert.AreEqual(WslConfigValidationSeverity.Warning, result.Issues[0].Severity);
        Assert.IsTrue(result.Issues[0].Message.Contains("Duplicate key"));
    }
}

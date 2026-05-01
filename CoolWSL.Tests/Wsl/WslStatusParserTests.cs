using CoolWSL.Wsl.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Wsl;

[TestClass]
public sealed class WslStatusParserTests
{
    private readonly WslStatusParser parser = new();

    [TestMethod]
    public void ParseStatus_ExtractsDocumentedFields()
    {
        const string output = """
Default Distribution: Ubuntu
Default Version: 2
Kernel version: 5.15.153.1-2
""";

        var result = parser.ParseStatus(output);

        Assert.AreEqual("Ubuntu", result.DefaultDistribution);
        Assert.AreEqual(2, result.DefaultVersion);
        Assert.AreEqual("5.15.153.1-2", result.KernelVersion);
        Assert.IsFalse(result.IsDegraded);
    }

    [TestMethod]
    public void ParseStatus_MarksUnknownFormatsAsDegraded()
    {
        const string output = "Etat actuel de WSL";

        var result = parser.ParseStatus(output);

        Assert.IsTrue(result.IsDegraded);
        StringAssert.Contains(result.DegradedReason!, "did not contain any key-value fields");
    }

    [TestMethod]
    public void ParseVersion_ExtractsVersionFields()
    {
        const string output = """
WSL version: 2.3.26.0
Kernel version: 5.15.153.1-2
Windows version: 10.0.26100.3915
""";

        var result = parser.ParseVersion(output);

        Assert.AreEqual("2.3.26.0", result.WslVersion);
        Assert.AreEqual("5.15.153.1-2", result.KernelVersion);
        Assert.AreEqual("10.0.26100.3915", result.WindowsVersion);
        Assert.IsFalse(result.IsDegraded);
    }
}
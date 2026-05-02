using CoolWSL.Core.Models;
using CoolWSL.Wsl.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Wsl;

[TestClass]
public sealed class WslListParserTests
{
    private readonly WslListParser parser = new();

    [TestMethod]
    public void Parse_ParsesDefaultRunningAndStoppedDistros()
    {
        const string output = """
Windows Subsystem for Linux Distributions:
  NAME              STATE           VERSION
* Ubuntu 22.04      Running         2
  Debian            Stopped         1
""";

        var result = parser.Parse(output);

        Assert.IsFalse(result.IsDegraded);
        Assert.AreEqual(2, result.Distros.Count);
        Assert.AreEqual("Ubuntu 22.04", result.Distros[0].Name);
        Assert.AreEqual(WslDistroState.Running, result.Distros[0].State);
        Assert.AreEqual(2, result.Distros[0].WslVersion);
        Assert.IsTrue(result.Distros[0].IsDefault);
        Assert.AreEqual(WslDistroState.Stopped, result.Distros[1].State);
    }

    [TestMethod]
    public void Parse_ReturnsEmptyForNoInstalledDistributions()
    {
        const string output = "Windows Subsystem for Linux has no installed distributions.";

        var result = parser.Parse(output);

        Assert.IsTrue(result.HasNoDistributions);
        Assert.AreEqual(0, result.Distros.Count);
        Assert.IsFalse(result.IsDegraded);
    }

    [TestMethod]
    public void Parse_MarksMissingVersionAsDegraded()
    {
        const string output = """
NAME             STATE
* Ubuntu 22.04   Running
""";

        var result = parser.Parse(output);

        Assert.IsTrue(result.IsDegraded);
        Assert.AreEqual(1, result.Distros.Count);
        Assert.IsNull(result.Distros[0].WslVersion);
    }

    [TestMethod]
    public void Parse_MarksUnknownStateAsDegradedButKeepsInventory()
    {
        const string output = """
NOM              ETAT            VERSION
* Ubuntu 22.04   En cours        2
""";

        var result = parser.Parse(output);

        Assert.IsTrue(result.IsDegraded);
        Assert.AreEqual(1, result.Distros.Count);
        Assert.AreEqual(WslDistroState.Unknown, result.Distros[0].State);
        Assert.AreEqual("En cours", result.Distros[0].StateLabel);
    }

    [TestMethod]
    public void InferStatesFromRunningList_MapsUnknownStatesToRunningAndStopped()
    {
        const string verboseOutput = """
NOM              ETAT            VERSION
* Ubuntu 22.04   En cours        2
  Debian         Arrete          1
""";
        const string runningOutput = "Ubuntu 22.04\n";

        var parseResult = parser.Parse(verboseOutput);
        var inferredResult = parser.InferStatesFromRunningList(parseResult, parser.ParseDistroNames(runningOutput));

        Assert.IsTrue(inferredResult.IsDegraded);
        Assert.AreEqual(WslDistroState.Running, inferredResult.Distros[0].State);
        Assert.AreEqual(WslDistroState.Stopped, inferredResult.Distros[1].State);
        Assert.AreEqual("En cours", inferredResult.Distros[0].StateLabel);
        Assert.AreEqual("Arrete", inferredResult.Distros[1].StateLabel);
    }
}
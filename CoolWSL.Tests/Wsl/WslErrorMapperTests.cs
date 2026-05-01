using CoolWSL.Core.Models;
using CoolWSL.Wsl.Errors;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Wsl;

[TestClass]
public sealed class WslErrorMapperTests
{
    private readonly WslErrorMapper mapper = new();

    [TestMethod]
    public void MapFailure_MapsMissingExecutableToNotInstalled()
    {
        var error = mapper.MapFailure(new WslCommand("wsl.exe", Array.Empty<string>()), string.Empty, string.Empty, null, new Win32Exception(2));

        Assert.AreEqual(WslErrorKind.NotInstalled, error.Kind);
    }

    [TestMethod]
    public void MapFailure_MapsUnsupportedCommandLineOption()
    {
        var error = mapper.MapFailure(
            new WslCommand("wsl.exe", new[] { "--version" }),
            string.Empty,
            "Invalid command line option: --version",
            1);

        Assert.AreEqual(WslErrorKind.UnsupportedFeature, error.Kind);
    }

    [TestMethod]
    public void MapFailure_MapsMissingDistribution()
    {
        var error = mapper.MapFailure(
            new WslCommand("wsl.exe", new[] { "--terminate", "Missing" }),
            string.Empty,
            "There is no distribution with the supplied name.",
            1);

        Assert.AreEqual(WslErrorKind.DistroNotFound, error.Kind);
    }
}
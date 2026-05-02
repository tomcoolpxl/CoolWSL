using CoolWSL.App.Models;
using CoolWSL.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.Distro;

[TestClass]
public sealed class DistroSelectionItemTests
{
    [TestMethod]
    public void Create_FormatsWslVersionWithExplicitLabel()
    {
        var item = DistroSelectionItem.Create(new WslDistro("Ubuntu", WslDistroState.Running, "Running", 2, true));

        Assert.AreEqual("WSL 2", item.WslVersionLabel);
    }

    [TestMethod]
    public void Create_UsesFallbackWhenWslVersionIsMissing()
    {
        var item = DistroSelectionItem.Create(new WslDistro("Ubuntu", WslDistroState.Running, "Running", null, false));

        Assert.AreEqual("WSL version not reported", item.WslVersionLabel);
    }
}
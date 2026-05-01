using CoolWSL.Wsl.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Wsl;

[TestClass]
public sealed class WslCommandFactoryTests
{
    [TestMethod]
    public void CreateListVerboseCommand_UsesDocumentedArguments()
    {
        var command = WslCommandFactory.CreateListVerboseCommand();

        Assert.AreEqual("wsl.exe", command.FileName);
        CollectionAssert.AreEqual(new[] { "--list", "--verbose" }, command.Arguments.ToArray());
        Assert.AreEqual("wsl.exe --list --verbose", command.CommandText);
    }

    [TestMethod]
    public void CreateTerminateDistroCommand_PreservesRawArgumentAndQuotesDisplayText()
    {
        var command = WslCommandFactory.CreateTerminateDistroCommand("Ubuntu Dev && calc");

        CollectionAssert.AreEqual(new[] { "--terminate", "Ubuntu Dev && calc" }, command.Arguments.ToArray());
        Assert.AreEqual("wsl.exe --terminate \"Ubuntu Dev && calc\"", command.CommandText);
    }
}
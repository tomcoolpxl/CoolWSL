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
    public void CreateListRunningQuietCommand_UsesDocumentedArguments()
    {
        var command = WslCommandFactory.CreateListRunningQuietCommand();

        Assert.AreEqual("wsl.exe", command.FileName);
        CollectionAssert.AreEqual(new[] { "--list", "--running", "--quiet" }, command.Arguments.ToArray());
        Assert.AreEqual("wsl.exe --list --running --quiet", command.CommandText);
    }

    [TestMethod]
    public void CreateTerminateDistroCommand_PreservesRawArgumentAndQuotesDisplayText()
    {
        var command = WslCommandFactory.CreateTerminateDistroCommand("Ubuntu Dev && calc");

        CollectionAssert.AreEqual(new[] { "--terminate", "Ubuntu Dev && calc" }, command.Arguments.ToArray());
        Assert.AreEqual("wsl.exe --terminate \"Ubuntu Dev && calc\"", command.CommandText);
    }

    [TestMethod]
    public void CreateOpenDefaultDistroCommand_UsesBareWslCommand()
    {
        var command = WslCommandFactory.CreateOpenDefaultDistroCommand();

        Assert.AreEqual("wsl.exe", command.FileName);
        CollectionAssert.AreEqual(Array.Empty<string>(), command.Arguments.ToArray());
        Assert.AreEqual("wsl.exe", command.CommandText);
    }

    [TestMethod]
    public void CreateStartDistroCommand_UsesNoOpShellCommand()
    {
        var command = WslCommandFactory.CreateStartDistroCommand("Ubuntu Dev");

        CollectionAssert.AreEqual(
            new[] { "--distribution", "Ubuntu Dev", "--exec", "/bin/sh", "-lc", ":" },
            command.Arguments.ToArray());
        Assert.AreEqual("wsl.exe --distribution \"Ubuntu Dev\" --exec /bin/sh -lc :", command.CommandText);
    }

    [TestMethod]
    public void CreateRunInDistroCommand_PreservesRawShellText()
    {
        var command = WslCommandFactory.CreateRunInDistroCommand("Ubuntu Dev", "echo \"hi\" && pwd");

        CollectionAssert.AreEqual(
            new[] { "--distribution", "Ubuntu Dev", "--exec", "/bin/sh", "-lc", "echo \"hi\" && pwd" },
            command.Arguments.ToArray());
        Assert.AreEqual("wsl.exe --distribution \"Ubuntu Dev\" --exec /bin/sh -lc \"echo \\\"hi\\\" && pwd\"", command.CommandText);
    }
}
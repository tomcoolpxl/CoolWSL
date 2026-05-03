using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.Distro;

[TestClass]
public sealed class DistroPageXamlTests
{
    [TestMethod]
    public void WslConfigKeyCard_UsesGridLengthResourceForSettingsColumn()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var appXamlPath = Path.Combine(repositoryRoot, "CoolWSL.App", "App.xaml");
        var keyCardXamlPath = Path.Combine(repositoryRoot, "CoolWSL.App", "Views", "Controls", "WslConfigKeyCard.xaml");

        var appXaml = File.ReadAllText(appXamlPath);
        var keyCardXaml = File.ReadAllText(keyCardXamlPath);

        StringAssert.Contains(appXaml, "<GridLength x:Key=\"SettingsValueColumnWidth\">280</GridLength>");
        StringAssert.Contains(keyCardXaml, "<ColumnDefinition Width=\"{StaticResource SettingsValueColumnWidth}\" />");
        Assert.IsFalse(
            keyCardXaml.Contains("<ColumnDefinition Width=\"{StaticResource SettingsValueControlWidth}\" />", StringComparison.Ordinal),
            "ColumnDefinition.Width must not use the Double-based SettingsValueControlWidth resource.");
    }

}

using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.Distro;

[TestClass]
public sealed class DistroSettingsRowViewModelTests
{
    private static readonly WslConfigKey SystemdKey = WslDistroConfigSchema.Current.Single(key => key.Section == "boot" && key.Key == "systemd");
    private static readonly WslDistroCapabilityContext Capabilities = new(26100, "2.5.9", 2, false, Array.Empty<string>());

    [TestMethod]
    public void ApplyProbeResult_FormatsActionableStatusText()
    {
        var row = new DistroSettingsRowViewModel(SystemdKey, static (_, _) => { });

        row.ApplyProbeResult(new WslConfigProbeResult(
            "boot.systemd-probe",
            "boot.systemd",
            WslConfigProbeStatus.NotEffective,
            "systemd is not active",
            "test -d /run/systemd/system",
            DateTimeOffset.UnixEpoch));

        Assert.AreEqual("Not active in the current distro session yet. Restart the distro if you just changed this setting.", row.ProbeStatusText);
        Assert.IsTrue(row.HasProbeEvidence);
        Assert.AreEqual("systemd is not active", row.ProbeEvidenceText);
    }

    [TestMethod]
    public void Refresh_ClearsStaleProbeResult()
    {
        var row = new DistroSettingsRowViewModel(SystemdKey, static (_, _) => { });
        row.ApplyProbeResult(new WslConfigProbeResult(
            "boot.systemd-probe",
            "boot.systemd",
            WslConfigProbeStatus.Effective,
            "active",
            "test -d /run/systemd/system",
            DateTimeOffset.UnixEpoch));

        row.Refresh(IniParser.Parse("[boot]\nsystemd=true\n"), Capabilities);

        Assert.IsFalse(row.HasProbeResult);
        Assert.AreEqual(string.Empty, row.ProbeStatusText);
    }
}
using CoolWSL.App.Models;
using CoolWSL.Core.Models;
using CoolWSL.Diagnostics.Status;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.App.Dashboard;

[TestClass]
public sealed class DashboardStateTests
{
    private static readonly DateTimeOffset RefreshedAt = new(2026, 5, 1, 9, 30, 0, TimeSpan.Zero);

    [TestMethod]
    public void Create_MapsHealthySnapshot()
    {
        var state = DashboardState.Create(
            new DashboardStatusSnapshot(
                new(
                    WslAvailability.Available,
                    "WSL is available.",
                    "Ubuntu",
                    2,
                    "6.6.87.2",
                    "2.5.9",
                    "10.0.26100",
                    false,
                    null,
                    null),
                new(
                    WslAvailability.Available,
                    new[]
                    {
                        new WslDistro("Ubuntu", WslDistroState.Running, "Running", 2, true),
                        new WslDistro("Debian", WslDistroState.Stopped, "Stopped", 1, false),
                    },
                    "Loaded WSL distro inventory.")),
            RefreshedAt);

        Assert.AreEqual("Installed", state.AvailabilityLabel);
        Assert.AreEqual("2.5.9", state.WslVersion);
        Assert.AreEqual("6.6.87.2", state.KernelVersion);
        Assert.AreEqual("2", state.DefaultWslVersion);
        Assert.IsTrue(state.HasDistroRows);
        Assert.AreEqual(2, state.Distros.Count);
        Assert.AreEqual("Default", state.Distros[0].DefaultLabel);
        Assert.AreEqual("WSL 2", state.Distros[0].WslVersion);
        Assert.AreEqual("WSL 1", state.Distros[1].WslVersion);
        Assert.AreEqual("2 registered distros. 1 running.", state.DistroSectionSummary);
        Assert.IsFalse(state.IsLoading);
        Assert.IsTrue(state.HasLoaded);
    }

    [TestMethod]
    public void Create_MapsNoDistroState()
    {
        var state = DashboardState.Create(
            new DashboardStatusSnapshot(
                new(
                    WslAvailability.Available,
                    "WSL is available.",
                    null,
                    2,
                    null,
                    "2.5.9",
                    null,
                    false,
                    null,
                    null),
                new(
                    WslAvailability.Available,
                    Array.Empty<WslDistro>(),
                    "WSL is available, but no Linux distributions are installed.")),
            RefreshedAt);

        Assert.IsTrue(state.ShowEmptyState);
        Assert.AreEqual("No distros installed", state.EmptyStateTitle);
        Assert.AreEqual("Install a Linux distribution to populate the dashboard inventory table.", state.EmptyStateMessage);
        Assert.AreEqual("No Linux distributions are installed.", state.DistroSectionSummary);
    }

    [TestMethod]
    public void Create_MapsUnavailableState()
    {
        var state = DashboardState.Create(
            new DashboardStatusSnapshot(
                new(
                    WslAvailability.NotInstalled,
                    "WSL is not installed on this machine.",
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    null,
                    "Install WSL and then refresh the dashboard."),
                new(
                    WslAvailability.NotInstalled,
                    Array.Empty<WslDistro>(),
                    "WSL is not installed on this machine.",
                    false,
                    null,
                    "Install WSL and then refresh the dashboard.")),
            RefreshedAt);

        Assert.AreEqual("Not installed", state.AvailabilityLabel);
        Assert.AreEqual("Unavailable", state.WslVersion);
        Assert.AreEqual("WSL is not installed", state.EmptyStateTitle);
        Assert.AreEqual("WSL is not installed on this machine.", state.EmptyStateMessage);
        Assert.AreEqual("Install WSL and then refresh the dashboard.", state.SuggestedNextStep);
    }

    [TestMethod]
    public void Create_SurfacesPartialFailureWithoutDroppingRows()
    {
        var state = DashboardState.Create(
            new DashboardStatusSnapshot(
                new(
                    WslAvailability.Available,
                    "WSL is available, but some environment details could not be determined safely.",
                    "Ubuntu",
                    null,
                    null,
                    null,
                    null,
                    true,
                    "The installed WSL version does not report full status metadata.",
                    "Run wsl --update and refresh if you need richer status details."),
                new(
                    WslAvailability.Available,
                    new[]
                    {
                        new WslDistro("Ubuntu", WslDistroState.Stopped, "Stopped", null, true),
                    },
                    "WSL distro inventory is only partially available.",
                    true,
                    "Verbose distro inventory is unavailable on this WSL version.",
                    "Refresh after updating WSL if you need full distro details.")),
            RefreshedAt);

        Assert.IsTrue(state.HasDistroRows);
        Assert.AreEqual(1, state.Distros.Count);
        Assert.IsTrue(state.HasWarning);
        StringAssert.Contains(state.WarningText ?? string.Empty, "The installed WSL version does not report full status metadata.");
        StringAssert.Contains(state.WarningText ?? string.Empty, "Verbose distro inventory is unavailable on this WSL version.");
        Assert.IsTrue(state.HasSuggestedNextStep);
    }
}
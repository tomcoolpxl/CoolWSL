using CoolWSL.App.ViewModels;
using CoolWSL.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Smoke;

[TestClass]
public sealed class ShellViewModelTests
{
    [TestMethod]
    public void ExposesExpectedTopLevelSections()
    {
        var viewModel = new ShellViewModel();

        var sections = viewModel.Sections.Select(item => item.Section).ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                AppSection.Dashboard,
                AppSection.Distros,
                AppSection.Diagnostics,
                AppSection.Logs,
                AppSection.Settings,
            },
            sections);
    }
}
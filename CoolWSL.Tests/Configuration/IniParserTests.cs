using CoolWSL.Core.Models.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoolWSL.Tests.Configuration;

[TestClass]
public class IniParserTests
{
    [TestMethod]
    public void EmptyFile_ReturnsEmptyDocument()
    {
        var doc = IniParser.Parse("");
        Assert.AreEqual(0, doc.Sections.Count);
        Assert.AreEqual("", doc.Serialize()); // Empty string for empty file
    }

    [TestMethod]
    public void SingleSection_SingleKey_RoundTrips()
    {
        var input = "[boot]\nsystemd=true\n";
        var doc = IniParser.Parse(input);
        
        Assert.AreEqual(1, doc.Sections.Count);
        Assert.AreEqual("boot", doc.Sections[0].Name);
        Assert.AreEqual(1, doc.Sections[0].Body.OfType<IniEntry>().Count());
        Assert.AreEqual("true", doc.Sections[0].Entry("systemd")!.Value);
        
        Assert.AreEqual(input, doc.Serialize());
    }

    [TestMethod]
    public void Comments_PreservedVerbatim()
    {
        var input = "# comment1\n; comment2\n[boot]\n# comment3\nsystemd=true\n";
        var doc = IniParser.Parse(input);
        Assert.AreEqual(input, doc.Serialize());
    }

    [TestMethod]
    public void MalformedLines_Preserved()
    {
        var input = "not a section\n[boot]\nsome text\nsystemd=true\n";
        var doc = IniParser.Parse(input);
        Assert.AreEqual(input, doc.Serialize());
    }

    [TestMethod]
    public void QuotedDrvFsOptions_PreservesRawValue_AndExposesEffectiveValue()
    {
        var input = "[automount]\noptions=\"metadata,uid=1003,gid=1003,umask=077,fmask=11,case=off\"\n";
        var doc = IniParser.Parse(input);
        var entry = doc.Sections[0].Entry("options")!;
        Assert.AreEqual("\"metadata,uid=1003,gid=1003,umask=077,fmask=11,case=off\"", entry.Value);
        Assert.AreEqual("metadata,uid=1003,gid=1003,umask=077,fmask=11,case=off", entry.EffectiveValue);
        
        Assert.AreEqual(input, doc.Serialize());

        var newEntry = new IniEntry {
            Key = "options",
            RawKey = "options",
            Value = "metadata, uid=1003, gid=1003, umask=077",
            OriginalValue = "metadata, uid=1003, gid=1003, umask=077",
            RawLine = null
        };
        var newDoc = doc.WithSection(doc.Sections[0].WithEntry(newEntry));
        var expectedNewOutput = "[automount]\noptions=\"metadata, uid=1003, gid=1003, umask=077\"\n";
        Assert.AreEqual(expectedNewOutput, newDoc.Serialize());
    }

    [TestMethod]
    public void QuotedFreeText_RetainsOuterQuotesInStoredValue()
    {
        var input = "[boot]\ncommand=\"echo \\\"hello\\\"\"\n";

        var doc = IniParser.Parse(input);
        var entry = doc.Sections[0].Entry("command")!;

        Assert.AreEqual("\"echo \\\"hello\\\"\"", entry.Value);
        Assert.AreEqual("echo \"hello\"", entry.EffectiveValue);
        Assert.AreEqual(input, doc.Serialize());
    }

    [TestMethod]
    public void DuplicateSections_Preserved()
    {
        var input = "[boot]\nsystemd=true\n[boot]\ncommand=test\n";
        var doc = IniParser.Parse(input);
        Assert.AreEqual(2, doc.Sections.Count);
        Assert.AreEqual(input, doc.Serialize());
    }
}

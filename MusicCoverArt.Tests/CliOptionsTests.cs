using System;
using Italbytz.Music.Abstractions;

namespace MusicCoverArt.Tests;

[TestClass]
public class CliOptionsTests
{
    [TestMethod]
    public void Parse_SetsDownloadIndexToZero_ForDownloadFirst()
    {
        var options = CliOptions.Parse(["search", "Daft", "Punk", "--download-first", "--limit", "3"]);

        Assert.IsNotNull(options);
        Assert.AreEqual("Daft Punk", options.Term);
        Assert.AreEqual(3, options.Limit);
        Assert.AreEqual(0, options.DownloadIndex);
    }

    [TestMethod]
    public void Parse_SetsExplicitDownloadIndex()
    {
        var options = CliOptions.Parse(["search", "Daft Punk", "--download-index", "2", "--size", "large"]);

        Assert.IsNotNull(options);
        Assert.AreEqual(2, options.DownloadIndex);
        Assert.AreEqual(CoverArtSize.Large, options.Size);
    }

    [TestMethod]
    public void Parse_ThrowsForConflictingDownloadModes()
    {
        ArgumentException? exception = null;

        try
        {
            CliOptions.Parse(["search", "Daft Punk", "--download-first", "--download-index", "1"]);
        }
        catch (ArgumentException caught)
        {
            exception = caught;
        }

        Assert.IsNotNull(exception);
        StringAssert.Contains(exception.Message, "cannot be combined");
    }
}
using Xunit;

namespace Cadroue.Tests;

public sealed class LosslessCutDiscoveryTests
{
    [Fact]
    public void AdjacentDiscovery_SelectsMatchingProject()
    {
        using var losslessCut = new TLosslessCut();
        string source = losslessCut.SourceCreate("clip.mp4");
        string expected = losslessCut.AdjacentCreate(
            "matching.llc",
            """{"version":1,"mediaFileName":"clip.mp4","cutSegments":[]}""");
        losslessCut.AdjacentCreate(
            "other.llc",
            """{"version":1,"mediaFileName":"other.mp4","cutSegments":[]}""");

        Assert.Equal(expected, Assert.Single(losslessCut.AdjacentRead(source)));
    }

    [Fact]
    public void NoAdjacentProject_ProducesCleanAbsence()
    {
        using var losslessCut = new TLosslessCut();
        string source = losslessCut.SourceCreate("clip.mp4");

        Assert.Empty(losslessCut.AdjacentRead(source));
    }

    [Fact]
    public void UnrelatedAdjacentFiles_AreIgnored()
    {
        using var losslessCut = new TLosslessCut();
        string source = losslessCut.SourceCreate("clip.mp4");
        losslessCut.AdjacentCreate("notes.txt", "not a project");
        losslessCut.AdjacentCreate("malformed.llc", "{ not json");
        losslessCut.AdjacentCreate(
            "different.llc",
            """{"version":1,"mediaFileName":"different.mp4","cutSegments":[]}""");

        Assert.Empty(losslessCut.AdjacentRead(source));
    }
}

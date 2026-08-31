using Xunit;

namespace Cadroue.Tests;

public sealed class TLosslesscutDiscovery
{
    [Fact]
    public void AdjacentDiscovery_SelectsMatchingProject()
    {
        using var losslessCut = new TLosslesscut();
        string source = losslessCut.TSourceCreate("clip.mp4");
        string expected = losslessCut.TLosslesscutAdjacentCreate(
            "matching.llc",
            """{"version":1,"mediaFileName":"clip.mp4","cutSegments":[]}""");
        losslessCut.TLosslesscutAdjacentCreate(
            "other.llc",
            """{"version":1,"mediaFileName":"other.mp4","cutSegments":[]}""");

        Assert.Equal(expected, Assert.Single(losslessCut.TLosslesscutAdjacentRead(source)));
    }

    [Fact]
    public void NoAdjacentProject_ProducesCleanAbsence()
    {
        using var losslessCut = new TLosslesscut();
        string source = losslessCut.TSourceCreate("clip.mp4");

        Assert.Empty(losslessCut.TLosslesscutAdjacentRead(source));
    }

    [Fact]
    public void UnrelatedAdjacentFiles_AreIgnored()
    {
        using var losslessCut = new TLosslesscut();
        string source = losslessCut.TSourceCreate("clip.mp4");
        losslessCut.TLosslesscutAdjacentCreate("notes.txt", "not a project");
        losslessCut.TLosslesscutAdjacentCreate("malformed.llc", "{ not json");
        losslessCut.TLosslesscutAdjacentCreate(
            "different.llc",
            """{"version":1,"mediaFileName":"different.mp4","cutSegments":[]}""");

        Assert.Empty(losslessCut.TLosslesscutAdjacentRead(source));
    }
}

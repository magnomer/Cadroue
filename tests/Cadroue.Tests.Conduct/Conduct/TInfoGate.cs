using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TInfoGate
{
    private readonly CInfo tInfoArea;

    public TInfoGate()
    {
        tInfoArea = new CAtelier(
                new TUsherPort(), new TLineagePort(), new TWorkspacePort(), new TPlacementPort(), new TViewerPort(),
                new TEditPort())
            .CAtelierInfo;
    }

    [Fact]
    public void InfoProbeCheck_EmptyPathOnly_Probes()
    {
        Assert.True(CInfo.CInfoProbeCheck(string.Empty));
        Assert.False(CInfo.CInfoProbeCheck(null));
        Assert.False(CInfo.CInfoProbeCheck(@"C:\clip.mp4"));
    }

    [Theory]
    [InlineData("", null, 1)]
    [InlineData("", true, 0)]
    [InlineData("", false, 0)]
    [InlineData(@"C:\clip.mp4", null, 0)]
    [InlineData(null, null, 0)]
    public void InfoProbeDefer_PathlessCargoWithoutAnswer_DefersOnce(
        string? sourcePath, bool? availability, int expected)
    {
        int defers = 0;

        tInfoArea.CInfoProbeDefer(sourcePath, availability, () => defers++);

        Assert.Equal(expected, defers);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData(@"C:\clip.mp4", 0)]
    [InlineData(null, 0)]
    public void InfoAvailabilityPropagate_PathlessCargoOnly_Changes(string? sourcePath, int expected)
    {
        int changes = 0;

        tInfoArea.CInfoAvailabilityPropagate(sourcePath, () => changes++);

        Assert.Equal(expected, changes);
    }
}

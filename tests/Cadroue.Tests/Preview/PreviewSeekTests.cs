using Xunit;

namespace Cadroue.Tests;

public sealed class PreviewSeekTests
{
    [Fact]
    public void PositionResolve_EndUsesLastFrameStart()
    {
        TimeSpan duration = TimeSpan.FromHours(2) + TimeSpan.FromMinutes(40);
        TimeSpan videoEnd = duration - TimeSpan.FromMilliseconds(33.367);

        TimeSpan position = TInterface.PreviewPositionResolve(duration, videoEnd);

        Assert.Equal(videoEnd, position);
    }

    [Fact]
    public void PositionResolve_FinalFrameIntervalUsesLastFrameStart()
    {
        TimeSpan duration = TimeSpan.FromSeconds(10);
        TimeSpan videoEnd = duration - TimeSpan.FromMilliseconds(40);

        TimeSpan position = TInterface.PreviewPositionResolve(
            duration - TimeSpan.FromMilliseconds(10),
            videoEnd);

        Assert.Equal(videoEnd, position);
    }

    [Fact]
    public void PositionResolve_EarlierPositionIsUnchanged()
    {
        TimeSpan position = TimeSpan.FromSeconds(5);

        TimeSpan resolved = TInterface.PreviewPositionResolve(
            position,
            TimeSpan.FromSeconds(9));

        Assert.Equal(position, resolved);
    }

    [Fact]
    public void PositionResolve_UnknownTimingIsUnchanged()
    {
        TimeSpan position = TimeSpan.FromSeconds(10);

        TimeSpan resolved = TInterface.PreviewPositionResolve(position, null);

        Assert.Equal(position, resolved);
    }

}

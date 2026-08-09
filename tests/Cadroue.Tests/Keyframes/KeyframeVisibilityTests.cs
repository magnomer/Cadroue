using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.KeyframeData;

namespace Cadroue.Tests;

public sealed class KeyframeVisibilityTests
{
    [Fact]
    public void VisibleKeyframes_WindowClampsToWorkingRange()
    {
        var spool = Spool(100, 200);
        var keyframes = new[] { Entry(90), Entry(150), Entry(210) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(150), spool);

        Assert.Single(result);
        Assert.Equal(At(150), result[0].LKeyframePresentationTime);
    }

    [Fact]
    public void VisibleKeyframes_WithCursorWindowOutsideWorkingRange_AreEmpty()
    {
        var spool = Spool(100, 200);
        var keyframes = new[] { Entry(150) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(2000), spool);

        Assert.Empty(result);
    }

    [Fact]
    public void VisibleKeyframes_ExcludeEntriesOutsideWindow()
    {
        var spool = Spool(0, 3600);
        var keyframes = new[] { Entry(100), Entry(1000), Entry(2000) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(1000), spool);

        Assert.Single(result);
        Assert.Equal(At(1000), result[0].LKeyframePresentationTime);
    }

    [Fact]
    public void VisibleKeyframes_IncludeBoundaryEntries()
    {
        var spool = Spool(0, 3600);
        var keyframes = new[] { Entry(600), Entry(1600) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(1000), spool);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void VisibleKeyframes_PreserveEntryOrder()
    {
        var spool = Spool(0, 3600);
        var keyframes = new[] { Entry(1050), Entry(1000), Entry(1100) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(1050), spool);

        Assert.Equal(
            new[] { At(1050), At(1000), At(1100) },
            result.Select(e => e.LKeyframePresentationTime).ToArray());
    }
}

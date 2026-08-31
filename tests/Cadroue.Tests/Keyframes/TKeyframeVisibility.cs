using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TKeyframeData;

namespace Cadroue.Tests;

public sealed class TKeyframeVisibility
{
    [Fact]
    public void VisibleKeyframes_WindowClampsToWorkingRange()
    {
        var spool = TKeyframeSpoolCreate(100, 200);
        var keyframes = new[] { TKeyframeEntryCreate(90), TKeyframeEntryCreate(150), TKeyframeEntryCreate(210) };

        var result = TInterface.TKeyframeVisibleResolve(keyframes, TKeyframeAtCreate(150), spool);

        Assert.Single(result);
        Assert.Equal(TKeyframeAtCreate(150), result[0].LKeyframePresentationTime);
    }

    [Fact]
    public void VisibleKeyframes_WithCursorWindowOutsideWorkingRange_AreEmpty()
    {
        var spool = TKeyframeSpoolCreate(100, 200);
        var keyframes = new[] { TKeyframeEntryCreate(150) };

        var result = TInterface.TKeyframeVisibleResolve(keyframes, TKeyframeAtCreate(2000), spool);

        Assert.Empty(result);
    }

    [Fact]
    public void VisibleKeyframes_ExcludeEntriesOutsideWindow()
    {
        var spool = TKeyframeSpoolCreate(0, 3600);
        var keyframes = new[] { TKeyframeEntryCreate(100), TKeyframeEntryCreate(1000), TKeyframeEntryCreate(2000) };

        var result = TInterface.TKeyframeVisibleResolve(keyframes, TKeyframeAtCreate(1000), spool);

        Assert.Single(result);
        Assert.Equal(TKeyframeAtCreate(1000), result[0].LKeyframePresentationTime);
    }

    [Fact]
    public void VisibleKeyframes_IncludeBoundaryEntries()
    {
        var spool = TKeyframeSpoolCreate(0, 3600);
        var keyframes = new[] { TKeyframeEntryCreate(600), TKeyframeEntryCreate(1600) };

        var result = TInterface.TKeyframeVisibleResolve(keyframes, TKeyframeAtCreate(1000), spool);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void VisibleKeyframes_PreserveEntryOrder()
    {
        var spool = TKeyframeSpoolCreate(0, 3600);
        var keyframes = new[] { TKeyframeEntryCreate(1050), TKeyframeEntryCreate(1000), TKeyframeEntryCreate(1100) };

        var result = TInterface.TKeyframeVisibleResolve(keyframes, TKeyframeAtCreate(1050), spool);

        Assert.Equal(
            new[] { TKeyframeAtCreate(1050), TKeyframeAtCreate(1000), TKeyframeAtCreate(1100) },
            result.Select(e => e.LKeyframePresentationTime).ToArray());
    }
}

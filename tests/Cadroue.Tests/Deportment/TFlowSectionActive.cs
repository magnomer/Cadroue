using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlowSectionActive
{
    private static LFlow TFlowBuild(string? path = "C:\\media\\clip.mp4")
    {
        LFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSourceSet(flow, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), path);
        return flow;
    }

    [Fact]
    public void EditCheck_RequiresSectionActiveAndEditable()
    {
        LFlow flow = TFlowBuild();
        List<bool> notices = [];
        TInterface.TFlowEditAttach(flow, editable => notices.Add(editable));

        Assert.False(TInterface.TFlowEditCheck(flow));

        TInterface.TFlowSectionSet(flow, true);
        Assert.True(TInterface.TFlowEditCheck(flow));

        Assert.True(TInterface.TFlowEditSet(flow, false));
        Assert.False(TInterface.TFlowEditSet(flow, false));
        Assert.False(TInterface.TFlowEditCheck(flow));
        Assert.Equal([false], notices);
    }

    [Fact]
    public void Source_ClampsCursor_AndMatchesCaseInsensitive()
    {
        LFlow flow = TFlowBuild();

        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(90));
        Assert.Equal(TimeSpan.FromSeconds(60), flow.LFlowCursor);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(-5));
        Assert.Equal(TimeSpan.Zero, flow.LFlowCursor);

        Assert.True(TInterface.TFlowSourceMatch(flow, "c:\\MEDIA\\clip.mp4"));
        Assert.False(TInterface.TFlowSourceMatch(flow, null));
        Assert.True(TInterface.TFlowSourceCheck(flow));
        Assert.True(TInterface.TFlowScanCheck(flow));

        TInterface.TFlowSourceClear(flow);
        Assert.False(TInterface.TFlowSourceCheck(flow));
        Assert.Null(flow.LFlowSpool);
        Assert.Equal(TimeSpan.Zero, flow.LFlowDuration);
    }

    [Fact]
    public void ScanCheck_FailsWithoutCommandOrAfterUnload()
    {
        LFlow flow = TFlowBuild();
        TInterface.TFlowCommandSet(flow, false);
        Assert.False(TInterface.TFlowScanCheck(flow));

        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowUnloadSet(flow);
        Assert.False(TInterface.TFlowScanCheck(flow));

        Assert.False(TInterface.TFlowSourceCheck(TFlowBuild("  ")));
    }

    [Fact]
    public void StampAndLosslesscut_ReportChangesOnly()
    {
        LFlow flow = TFlowBuild();

        Assert.True(TInterface.TFlowStampSet(flow, "a/1/1"));
        Assert.False(TInterface.TFlowStampSet(flow, "a/1/1"));
        Assert.True(TInterface.TFlowStampSet(flow, "a/2/1"));

        Assert.True(TInterface.TFlowLosslesscutSet(flow, "C:\\media\\clip.mp4"));
        Assert.False(TInterface.TFlowLosslesscutSet(flow, "c:\\media\\CLIP.mp4"));
        Assert.True(TInterface.TFlowLosslesscutSet(flow, string.Empty));
    }
}

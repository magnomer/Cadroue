using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TFlowSectionActive
{
    private static QFlow TFlowBuild(string? path = "C:\\media\\clip.mp4")
    {
        QFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSourceSet(flow, TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), path);
        return flow;
    }

    [Fact]
    public void EditCheck_RequiresSectionActiveAndEditable()
    {
        QFlow flow = TFlowBuild();
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
        QFlow flow = TFlowBuild();

        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(90));
        Assert.Equal(TimeSpan.FromSeconds(60), flow.QFlowCursor);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(-5));
        Assert.Equal(TimeSpan.Zero, flow.QFlowCursor);

        Assert.True(TInterface.TFlowSourceMatch(flow, "c:\\MEDIA\\clip.mp4"));
        Assert.False(TInterface.TFlowSourceMatch(flow, null));
        Assert.True(TInterface.TFlowSourceCheck(flow));
        Assert.True(TInterface.TFlowScanCheck(flow));

        TInterface.TFlowSourceClear(flow);
        Assert.False(TInterface.TFlowSourceCheck(flow));
        Assert.Null(flow.QFlowRange);
        Assert.Equal(TimeSpan.Zero, flow.QFlowDuration);
    }

    [Fact]
    public void ScanCheck_FailsWithoutCommandOrAfterUnload()
    {
        QFlow flow = TFlowBuild();
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
        QFlow flow = TFlowBuild();

        Assert.True(TInterface.TFlowStampSet(flow, "a/1/1"));
        Assert.False(TInterface.TFlowStampSet(flow, "a/1/1"));
        Assert.True(TInterface.TFlowStampSet(flow, "a/2/1"));

        Assert.True(TInterface.TFlowLosslesscutSet(flow, "C:\\media\\clip.mp4"));
        Assert.False(TInterface.TFlowLosslesscutSet(flow, "c:\\media\\CLIP.mp4"));
        Assert.True(TInterface.TFlowLosslesscutSet(flow, string.Empty));
    }
}

using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TMonitorCursor
{
    [Fact]
    public void CursorSet_RaisesAndStores()
    {
        using LSMonitor monitor = TInterface.TMonitorCreate();
        TimeSpan seen = TimeSpan.Zero;
        TInterface.TMonitorCursorAttach(monitor, cursor => seen = cursor);

        TInterface.TMonitorCursorSet(monitor, TimeSpan.FromSeconds(12));

        Assert.Equal(TimeSpan.FromSeconds(12), monitor.LSMonitorCursor);
        Assert.Equal(TimeSpan.FromSeconds(12), seen);
    }

    [Fact]
    public void Zoom_KeepsCenter_ClampsScale()
    {
        using LSMonitor monitor = TInterface.TMonitorCreate();
        int zooms = 0;
        TInterface.TMonitorZoomAttach(monitor, () => zooms++);

        TInterface.TMonitorZoom(monitor, 2, 32);
        Assert.Equal(2, monitor.LSMonitorScale);
        Assert.Equal(0.25, monitor.LSMonitorOffset, 6);

        TInterface.TMonitorZoom(monitor, 0.5, 32);
        Assert.Equal(1, monitor.LSMonitorScale);
        Assert.Equal(0, monitor.LSMonitorOffset, 6);

        TInterface.TMonitorZoom(monitor, 64, 32);
        Assert.Equal(32, monitor.LSMonitorScale);
        Assert.Equal(3, zooms);
    }

    [Fact]
    public void Offset_ClampsToViewport_ResolvesFractions()
    {
        using LSMonitor monitor = TInterface.TMonitorCreate();
        TInterface.TMonitorZoom(monitor, 4, 32);

        TInterface.TMonitorOffsetSet(monitor, 0.9);
        Assert.Equal(0.75, monitor.LSMonitorOffset, 6);
        Assert.Equal(0.875, TInterface.TMonitorFractionResolve(monitor, 0.5), 6);
        Assert.Equal(0.5, TInterface.TMonitorLocalResolve(monitor, 0.875), 6);
        Assert.Equal(1, TInterface.TMonitorFractionResolve(monitor, 2), 6);
    }

    [Fact]
    public void ColumnRead_PeaksWithinVisibleSlice()
    {
        using LSMonitor monitor = TInterface.TMonitorCreate();
        double[] envelope = [0.1, 0.9, 0.2, 0.3];

        Assert.Equal(0.9, TInterface.TMonitorColumnRead(monitor, envelope, 0, 2));
        Assert.Equal(0.3, TInterface.TMonitorColumnRead(monitor, envelope, 1, 2));

        TInterface.TMonitorZoom(monitor, 2, 32);
        TInterface.TMonitorOffsetSet(monitor, 0.5);
        Assert.Equal(0.2, TInterface.TMonitorColumnRead(monitor, envelope, 0, 2));
        Assert.Equal(0.3, TInterface.TMonitorColumnRead(monitor, envelope, 1, 2));
    }

    [Fact]
    public void ZoomInOut_StepByTwo_ScrollFollows()
    {
        using LSMonitor monitor = TInterface.TMonitorCreate();

        TInterface.TMonitorIncreaseZoom(monitor);
        Assert.Equal(2, monitor.LSMonitorScale);
        LSMonitorScroll scroll = TInterface.TMonitorScrollRead(monitor);
        Assert.Equal(0.5, scroll.LSMonitorScrollViewport, 6);
        Assert.Equal(0.5, scroll.LSMonitorScrollMaximum, 6);
        Assert.True(scroll.LSMonitorScrollEnabled);
        Assert.Equal(1, scroll.LSMonitorScrollOpacity);

        TInterface.TMonitorDecreaseZoom(monitor);
        scroll = TInterface.TMonitorScrollRead(monitor);
        Assert.False(scroll.LSMonitorScrollEnabled);
        Assert.Equal(0.35, scroll.LSMonitorScrollOpacity);
    }

    [Fact]
    public void Head_AndSeek_UseGutterAndDuration()
    {
        using LSMonitor monitor = TInterface.TMonitorCreate();
        TimeSpan duration = TimeSpan.FromSeconds(100);
        double width = LSMonitorPlan.LSMonitorGutter + 200;

        Assert.False(TInterface.TMonitorHeadResolve(monitor, width, TimeSpan.Zero).LSMonitorHeadShown);
        TInterface.TMonitorCursorSet(monitor, TimeSpan.FromSeconds(50));
        LSMonitorHead head = TInterface.TMonitorHeadResolve(monitor, width, duration);
        Assert.True(head.LSMonitorHeadShown);
        Assert.Equal(LSMonitorPlan.LSMonitorGutter + 100, head.LSMonitorHeadLeft, 6);

        TimeSpan? seen = null;
        TInterface.TMonitorSeekAttach(monitor, cursor => seen = cursor);
        TInterface.TMonitorSeekHandle(monitor, false, LSMonitorPlan.LSMonitorGutter + 50, width, duration);
        Assert.Null(seen);
        TInterface.TMonitorSeekHandle(monitor, true, LSMonitorPlan.LSMonitorGutter + 50, width, duration);
        Assert.Equal(TimeSpan.FromSeconds(25), seen);
        TInterface.TMonitorSeekHandle(monitor, true, 0, width, TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromSeconds(25), seen);
    }

    [Fact]
    public void PlayHandle_AndBypass_RaiseByState()
    {
        using LSMonitor monitor = TInterface.TMonitorCreate();
        int plays = 0;
        int pauses = 0;
        TInterface.TMonitorPlayAttach(monitor, () => plays++, () => pauses++);

        TInterface.TMonitorPlayHandle(monitor);
        TInterface.TMonitorPlayingSet(monitor, true);
        TInterface.TMonitorPlayHandle(monitor);
        Assert.Equal(1, plays);
        Assert.Equal(1, pauses);
        Assert.Equal("PCompassPause.svg", TInterface.TMonitorFaceRead(monitor).LSMonitorFaceIcon);

        bool? bypass = null;
        TInterface.TMonitorBypassAttach(monitor, value => bypass = value);
        TInterface.TMonitorBypassSet(monitor, true);
        TInterface.TMonitorRadioHandle(monitor, true);
        Assert.Null(bypass);
        TInterface.TMonitorRadioHandle(monitor, false);
        Assert.False(bypass);
    }

    [Fact]
    public void Frame_EmptyEnvelope_StillDrawsGrid()
    {
        using LSMonitor monitor = TInterface.TMonitorCreate();

        LSMonitorFrame frame = TInterface.TMonitorFrameResolve(monitor, false, 246, 100);
        Assert.Empty(frame.LSMonitorFrameOutline.LFlowOutlinePoints);
        Assert.Equal(new[] { 0.0, 100, 25, 75, 50 }, frame.LSMonitorFrameLines);
        Assert.Equal(3, frame.LSMonitorFrameLabels.Count);
        Assert.Equal("0 dB", frame.LSMonitorFrameLabels[0].LSMonitorLabelText);
        Assert.Equal(0, frame.LSMonitorFrameLabels[0].LSMonitorLabelTop);
        Assert.Equal(43, frame.LSMonitorFrameLabels[2].LSMonitorLabelTop);
        Assert.Equal(
            TInterface.TLocalizationTextRead("NormalizePreview.Empty"), TInterface.TMonitorStatusRead(monitor, false));
    }

    [Fact]
    public void Outline_PeaksScaledToHalfHeight()
    {
        double[] envelope = [0.5, 1.0];

        LFlowWaveformOutline outline = TInterface.TMonitorOutlineResolve(
            envelope, LSMonitorPlan.LSMonitorGutter + 2, 100, 1, 0);

        Assert.Equal(4, outline.LFlowOutlinePoints.Count);
        Assert.Equal(LSMonitorPlan.LSMonitorGutter, outline.LFlowOutlineX);
        Assert.Equal(50, outline.LFlowOutlineY);
        Assert.Equal(25, outline.LFlowOutlinePoints[0].LFlowPointY);
        Assert.Equal(0, outline.LFlowOutlinePoints[1].LFlowPointY);
        Assert.Equal(100, outline.LFlowOutlinePoints[2].LFlowPointY);
        Assert.Equal(75, outline.LFlowOutlinePoints[3].LFlowPointY);
    }
}

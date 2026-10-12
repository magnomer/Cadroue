using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TMonitorCursor
{
    [Fact]
    public void CursorSet_RaisesAndStores()
    {
        using QSMonitor monitor = TInterface.TMonitorCreate();
        TimeSpan seen = TimeSpan.Zero;
        TInterface.TMonitorCursorAttach(monitor, cursor => seen = cursor);

        TInterface.TMonitorCursorSet(monitor, TimeSpan.FromSeconds(12));

        Assert.Equal(TimeSpan.FromSeconds(12), monitor.QSMonitorCursor);
        Assert.Equal(TimeSpan.FromSeconds(12), seen);
    }

    [Fact]
    public void Zoom_KeepsCenter_ClampsScale()
    {
        using QSMonitor monitor = TInterface.TMonitorCreate();
        int zooms = 0;
        TInterface.TMonitorZoomAttach(monitor, () => zooms++);

        TInterface.TMonitorZoom(monitor, 2, 32);
        Assert.Equal(2, monitor.QSMonitorScale);
        Assert.Equal(0.25, monitor.QSMonitorOffset, 6);

        TInterface.TMonitorZoom(monitor, 0.5, 32);
        Assert.Equal(1, monitor.QSMonitorScale);
        Assert.Equal(0, monitor.QSMonitorOffset, 6);

        TInterface.TMonitorZoom(monitor, 64, 32);
        Assert.Equal(32, monitor.QSMonitorScale);
        Assert.Equal(3, zooms);
    }

    [Fact]
    public void Offset_ClampsToViewport_ResolvesFractions()
    {
        using QSMonitor monitor = TInterface.TMonitorCreate();
        TInterface.TMonitorZoom(monitor, 4, 32);

        TInterface.TMonitorOffsetSet(monitor, 0.9);
        Assert.Equal(0.75, monitor.QSMonitorOffset, 6);
        Assert.Equal(0.875, TInterface.TMonitorFractionResolve(monitor, 0.5), 6);
        Assert.Equal(0.5, TInterface.TMonitorLocalResolve(monitor, 0.875), 6);
        Assert.Equal(1, TInterface.TMonitorFractionResolve(monitor, 2), 6);
    }

    [Fact]
    public void ColumnRead_PeaksWithinVisibleSlice()
    {
        using QSMonitor monitor = TInterface.TMonitorCreate();
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
        using QSMonitor monitor = TInterface.TMonitorCreate();

        TInterface.TMonitorIncreaseZoom(monitor);
        Assert.Equal(2, monitor.QSMonitorScale);
        QSMonitorScrollbar scroll = TInterface.TMonitorScrollRead(monitor);
        Assert.Equal(0.5, scroll.QSMonitorScrollViewport, 6);
        Assert.Equal(0.5, scroll.QSMonitorScrollMaximum, 6);
        Assert.True(scroll.QSMonitorScrollEnabled);
        Assert.Equal(1, scroll.QSMonitorScrollOpacity);

        TInterface.TMonitorDecreaseZoom(monitor);
        scroll = TInterface.TMonitorScrollRead(monitor);
        Assert.False(scroll.QSMonitorScrollEnabled);
        Assert.Equal(0.35, scroll.QSMonitorScrollOpacity);
    }

    [Fact]
    public void Head_AndSeek_UseGutterAndDuration()
    {
        using QSMonitor monitor = TInterface.TMonitorCreate();
        TimeSpan duration = TimeSpan.FromSeconds(100);
        double width = QSMonitorPlan.QSMonitorGutter + 200;

        Assert.False(TInterface.TMonitorHeadResolve(monitor, width, TimeSpan.Zero).QSMonitorHeadShown);
        TInterface.TMonitorCursorSet(monitor, TimeSpan.FromSeconds(50));
        QSMonitorHead head = TInterface.TMonitorHeadResolve(monitor, width, duration);
        Assert.True(head.QSMonitorHeadShown);
        Assert.Equal(QSMonitorPlan.QSMonitorGutter + 100, head.QSMonitorHeadLeft, 6);

        TimeSpan? seen = null;
        TInterface.TMonitorSeekAttach(monitor, cursor => seen = cursor);
        TInterface.TMonitorSeekHandle(monitor, false, QSMonitorPlan.QSMonitorGutter + 50, width, duration);
        Assert.Null(seen);
        TInterface.TMonitorSeekHandle(monitor, true, QSMonitorPlan.QSMonitorGutter + 50, width, duration);
        Assert.Equal(TimeSpan.FromSeconds(25), seen);
        TInterface.TMonitorSeekHandle(monitor, true, 0, width, TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromSeconds(25), seen);
    }

    [Fact]
    public void PlayHandle_AndBypass_RaiseByState()
    {
        using QSMonitor monitor = TInterface.TMonitorCreate();
        int plays = 0;
        int pauses = 0;
        TInterface.TMonitorPlayAttach(monitor, () => plays++, () => pauses++);

        TInterface.TMonitorPlayHandle(monitor);
        TInterface.TMonitorPlayingSet(monitor, true);
        TInterface.TMonitorPlayHandle(monitor);
        Assert.Equal(1, plays);
        Assert.Equal(1, pauses);
        Assert.Equal("PCompassPause.svg", TInterface.TMonitorFaceRead(monitor).QSMonitorFaceIcon);

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
        using QSMonitor monitor = TInterface.TMonitorCreate();

        QSMonitorFrame frame = TInterface.TMonitorFrameResolve(monitor, false, 246, 100);
        Assert.Empty(frame.QSMonitorFrameOutline.QFlowOutlinePoints);
        Assert.Equal(new[] { 0.0, 100, 25, 75, 50 }, frame.QSMonitorFrameLines);
        Assert.Equal(3, frame.QSMonitorFrameLabels.Count);
        Assert.Equal("0 dB", frame.QSMonitorFrameLabels[0].QSMonitorLabelText);
        Assert.Equal(0, frame.QSMonitorFrameLabels[0].QSMonitorLabelTop);
        Assert.Equal(43, frame.QSMonitorFrameLabels[2].QSMonitorLabelTop);
        string empty = Interface.TInterface.TLocalizationTextRead("NormalizePreview.Empty");
        Assert.Equal(empty, TInterface.TMonitorStatusRead(monitor, false));
    }

    [Fact]
    public void Outline_PeaksScaledToHalfHeight()
    {
        double[] envelope = [0.5, 1.0];

        QFlowWaveformOutline outline = TInterface.TMonitorOutlineResolve(
            envelope, QSMonitorPlan.QSMonitorGutter + 2, 100, 1, 0);

        Assert.Equal(4, outline.QFlowOutlinePoints.Count);
        Assert.Equal(QSMonitorPlan.QSMonitorGutter, outline.QFlowOutlineX);
        Assert.Equal(50, outline.QFlowOutlineY);
        Assert.Equal(25, outline.QFlowOutlinePoints[0].QFlowPointY);
        Assert.Equal(0, outline.QFlowOutlinePoints[1].QFlowPointY);
        Assert.Equal(100, outline.QFlowOutlinePoints[2].QFlowPointY);
        Assert.Equal(75, outline.QFlowOutlinePoints[3].QFlowPointY);
    }
}

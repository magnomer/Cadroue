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
}

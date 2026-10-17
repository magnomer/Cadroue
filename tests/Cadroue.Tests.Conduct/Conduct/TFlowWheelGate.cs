using System;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TFlowWheelGate
{
    private readonly TFlowPort tFlowWheelPort = new();
    private readonly CFlow tFlowWheelOwner;
    private readonly CFlowWheel tFlowWheelGate;

    public TFlowWheelGate()
    {
        tFlowWheelOwner = TAtelier.TAtelierCreate(port => port with { LAtelierFlow = tFlowWheelPort })
            .CAtelierWorkspaceCreate()
            .CWorkspaceFlow;
        tFlowWheelGate = tFlowWheelOwner.CFlowWheel;
    }

    [Fact]
    public void WheelRun_StoppedOrStill_RefusesWithoutReading()
    {
        Assert.False(tFlowWheelGate.CFlowWheelRun(120, 1));

        tFlowWheelOwner.CFlowCommandSet(true);

        Assert.False(tFlowWheelGate.CFlowWheelRun(0, -1));
        Assert.DoesNotContain("wheel", tFlowWheelPort.TFlowCalls);
    }

    [Fact]
    public void WheelRun_Seek_MovesTheCursorByTheStepAndSeeks()
    {
        TimeSpan? sought = null;
        tFlowWheelOwner.CFlowCursorSought += cursor => sought = cursor;
        tFlowWheelOwner.CFlowCommandSet(true);
        tFlowWheelOwner.CFlowAttach(null!, "full:a", TimeSpan.FromSeconds(10));

        Assert.True(tFlowWheelGate.CFlowWheelRun(-240, -2));

        Assert.Equal(TimeSpan.FromSeconds(6), tFlowWheelOwner.CFlowCursor);
        Assert.Equal(TimeSpan.FromSeconds(6), sought);
        Assert.Contains("step:-2", tFlowWheelPort.TFlowCalls);
    }

    [Fact]
    public void WheelRun_SeekWithoutARange_SeeksNothing()
    {
        TimeSpan? sought = null;
        tFlowWheelOwner.CFlowCursorSought += cursor => sought = cursor;
        tFlowWheelOwner.CFlowCommandSet(true);

        Assert.True(tFlowWheelGate.CFlowWheelRun(120, 1));

        Assert.Null(sought);
        Assert.DoesNotContain("step:1", tFlowWheelPort.TFlowCalls);
    }

    [Fact]
    public void WheelRun_Zoom_ZoomsAboutTheCursorAndTells()
    {
        int zoomed = 0;
        tFlowWheelGate.CFlowWheelZoomed += () => zoomed++;
        tFlowWheelPort.TFlowWheel = "Zoom";
        tFlowWheelOwner.CFlowCommandSet(true);
        tFlowWheelGate.CFlowWheelZoom(1);
        tFlowWheelOwner.CFlowAttach(null!, "full:a", TimeSpan.FromSeconds(30));

        Assert.True(tFlowWheelGate.CFlowWheelRun(120, 1));

        Assert.Equal(1, zoomed);
        Assert.Contains("zoom:30:1", tFlowWheelPort.TFlowCalls);
        Cadroue.Core.LSpool range = tFlowWheelOwner.CFlowRange!;
        Assert.True(range.LSpoolRangeLimit - range.LSpoolRangeOrigin < TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void WheelRun_Volume_AsksFiveAStep()
    {
        double? volume = null;
        tFlowWheelGate.CFlowWheelAdjusted += change => volume = change;
        tFlowWheelPort.TFlowWheel = "Volume";
        tFlowWheelOwner.CFlowCommandSet(true);

        Assert.True(tFlowWheelGate.CFlowWheelRun(-120, -1));

        Assert.Equal(-5, volume);
    }
}

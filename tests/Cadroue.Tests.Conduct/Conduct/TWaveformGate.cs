using System;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TWaveformGate
{
    private readonly TFlowPort tWaveformFlowPort = new() { TFlowWaveform = true };
    private readonly CFlow tWaveformFlow;
    private readonly CWaveform tWaveformGate;
    private readonly TWaveformPort tWaveformPort;
    private int tWaveformArrivals;
    private int tWaveformChanges;

    public TWaveformGate()
    {
        tWaveformFlow = TAtelier.TAtelierCreate(port => port with { LAtelierFlow = tWaveformFlowPort })
            .CAtelierWorkspaceCreate()
            .CWorkspaceFlow;
        tWaveformGate = tWaveformFlow.CFlowWaveform;
        tWaveformPort = tWaveformFlowPort.TFlowWaveformPort;
        tWaveformGate.CWaveformArrived += () => tWaveformArrivals++;
        tWaveformGate.CWaveformChanged += () => tWaveformChanges++;
    }

    [Fact]
    public void WaveformStart_OnAndLoaded_ScansTheHeldSource()
    {
        tWaveformFlow.CFlowCommandSet(true);
        tWaveformFlow.CFlowAttach(null!, "full:a", TimeSpan.Zero);

        tWaveformGate.CWaveformStart();

        Assert.Equal(["start:full:a:60:True"], tWaveformPort.TWaveformCalls);
    }

    [Fact]
    public void WaveformStart_OffOrUnloaded_DoesNotScan()
    {
        tWaveformFlow.CFlowWaveformSet(false);
        tWaveformGate.CWaveformStart();
        tWaveformFlow.CFlowWaveformSet(true);
        tWaveformFlow.CFlowUnloadSet();
        tWaveformPort.TWaveformCalls.Clear();

        tWaveformGate.CWaveformStart();

        Assert.Empty(tWaveformPort.TWaveformCalls);
    }

    [Fact]
    public void FlowWaveformSet_Changed_RepaintsThenScans_RepeatIsSilent()
    {
        Assert.True(tWaveformFlow.CFlowWaveformSet(false));
        Assert.False(tWaveformFlow.CFlowWaveformSet(false));
        Assert.Equal(1, tWaveformChanges);
        Assert.Empty(tWaveformPort.TWaveformCalls);

        Assert.True(tWaveformFlow.CFlowWaveformSet(true));

        Assert.Equal(2, tWaveformChanges);
        Assert.Equal(["start::0:False"], tWaveformPort.TWaveformCalls);
    }

    [Fact]
    public void WaveformArrived_Unloaded_IsDropped_ApplyIsSilent()
    {
        tWaveformPort.TWaveformRaise();
        Assert.Equal(1, tWaveformArrivals);

        tWaveformFlow.CFlowUnloadSet();
        tWaveformPort.TWaveformRaise();
        tWaveformGate.CWaveformApply();

        Assert.Equal(1, tWaveformArrivals);
        Assert.Equal(0, tWaveformChanges);
    }

    [Fact]
    public void WaveformShown_OnlyWhileOnAndPeaksHeld()
    {
        Assert.False(tWaveformGate.CWaveformShown);
        tWaveformPort.TWaveformPeaks = [0.5];
        Assert.True(tWaveformGate.CWaveformShown);

        tWaveformFlow.CFlowWaveformSet(false);
        tWaveformPort.TWaveformCalls.Clear();

        Assert.False(tWaveformGate.CWaveformShown);
        Assert.Empty(tWaveformPort.TWaveformCalls);
    }

    [Fact]
    public void WaveformOutlineResolve_TooNarrowOrTooLow_IsFlat_NoPeaks_IsAtTheOrigin()
    {
        TimeSpan end = TimeSpan.FromSeconds(1);
        tWaveformPort.TWaveformPeaks = [1, 1];
        var narrow = tWaveformGate.CWaveformOutlineResolve(1, 10, 20, TimeSpan.Zero, end);
        Assert.Empty(narrow.CWaveformPoints);
        Assert.Equal(0, narrow.CWaveformOriginX);
        Assert.Equal(20, narrow.CWaveformOriginY);

        var flat = tWaveformGate.CWaveformOutlineResolve(40, 10, 2, TimeSpan.Zero, end);
        Assert.Empty(flat.CWaveformPoints);
        Assert.Equal(11, flat.CWaveformOriginY);

        tWaveformPort.TWaveformPeaks = [];
        var silent = tWaveformGate.CWaveformOutlineResolve(40, 10, 20, TimeSpan.Zero, end);
        Assert.Empty(silent.CWaveformPoints);
        Assert.Equal((0d, 0d), (silent.CWaveformOriginX, silent.CWaveformOriginY));
        Assert.Equal(["range:0:1:1", "range:0:1:40", "range:0:1:40"], tWaveformPort.TWaveformCalls);
    }

    [Fact]
    public void WaveformOutlineResolve_WalksTheTopLeftToRight_ThenTheBottomBack()
    {
        tWaveformPort.TWaveformPeaks = [1, 1, 1, 1];

        var outline = tWaveformGate.CWaveformOutlineResolve(4, 0, 10, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        Assert.Equal(0, outline.CWaveformOriginX);
        Assert.Equal(1, outline.CWaveformOriginY);
        Assert.Equal(7, outline.CWaveformPoints.Count);
        Assert.Equal((3d, 1d), outline.CWaveformPoints[2]);
        Assert.Equal((3d, 9d), outline.CWaveformPoints[3]);
        Assert.Equal((0d, 9d), outline.CWaveformPoints[6]);
    }

    [Fact]
    public void WaveformOutlineResolve_Off_ReadsNoLevels()
    {
        tWaveformPort.TWaveformPeaks = [1, 1, 1, 1];
        tWaveformFlow.CFlowWaveformSet(false);

        var outline = tWaveformGate.CWaveformOutlineResolve(4, 0, 10, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        Assert.Empty(outline.CWaveformPoints);
        Assert.Equal(0, outline.CWaveformOriginY);
        Assert.DoesNotContain(tWaveformPort.TWaveformCalls, call => call.StartsWith("range", StringComparison.Ordinal));
    }

    [Fact]
    public void FlowClearAndClose_ClearThenCloseTheScan()
    {
        tWaveformFlow.CFlowCommandSet(true);
        tWaveformFlow.CFlowAttach(null!, "full:a", TimeSpan.Zero);
        tWaveformPort.TWaveformCalls.Clear();

        Assert.True(tWaveformFlow.CFlowClear(true));
        Assert.True(tWaveformFlow.CFlowClose());

        Assert.Equal(["clear", "close"], tWaveformPort.TWaveformCalls);
    }

    [Fact]
    public void FlowHeight_ReadsThePort()
    {
        Assert.Equal(180, tWaveformFlow.CFlowHeight);
        Assert.Contains("height", tWaveformFlowPort.TFlowCalls);
    }
}

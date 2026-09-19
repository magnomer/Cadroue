using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlowWaveformOutline
{
    [Fact]
    public void Outline_IsEmpty_WhenTooNarrowOrTooShortOrNoPeaks()
    {
        TimeSpan end = TimeSpan.FromSeconds(1);
        LFlowWaveformOutline narrow = TInterface.TFlowOutlineResolve([100, 200], 1, 10, 20, TimeSpan.Zero, end);
        Assert.Empty(narrow.LFlowOutlinePoints);
        Assert.Equal(0, narrow.LFlowOutlineX);
        Assert.Equal(20, narrow.LFlowOutlineY);

        LFlowWaveformOutline flat = TInterface.TFlowOutlineResolve([100, 200], 40, 10, 2, TimeSpan.Zero, end);
        Assert.Empty(flat.LFlowOutlinePoints);

        LFlowWaveformOutline silent = TInterface.TFlowOutlineResolve([], 40, 10, 20, TimeSpan.Zero, end);
        Assert.Empty(silent.LFlowOutlinePoints);
    }

    [Fact]
    public void Outline_WalksTheTopLeftToRight_ThenTheBottomBack()
    {
        byte[] peaks = new byte[25];
        Array.Fill(peaks, (byte)255);
        LFlowWaveformOutline outline = TInterface.TFlowOutlineResolve(
            peaks, 4, 0, 10, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        Assert.Equal(0, outline.LFlowOutlineX);
        Assert.Equal(1, outline.LFlowOutlineY);
        Assert.Equal(7, outline.LFlowOutlinePoints.Count);
        Assert.Equal(3, outline.LFlowOutlinePoints[2].LFlowPointX);
        Assert.Equal(1, outline.LFlowOutlinePoints[2].LFlowPointY);
        Assert.Equal(3, outline.LFlowOutlinePoints[3].LFlowPointX);
        Assert.Equal(9, outline.LFlowOutlinePoints[3].LFlowPointY);
        Assert.Equal(0, outline.LFlowOutlinePoints[6].LFlowPointX);
        Assert.Equal(9, outline.LFlowOutlinePoints[6].LFlowPointY);
    }

    [Fact]
    public void Peaks_AreEmptyWhileTheWaveformIsOff()
    {
        LFlow flow = TInterface.TFlowCreate();
        List<int> updates = [];
        TInterface.TFlowWaveformAttach(flow, () => { }, peaks => updates.Add(peaks.Length));
        bool active = flow.LFlowWaveformActive;

        Assert.False(TInterface.TFlowWaveformSet(flow, active));
        Assert.True(TInterface.TFlowWaveformSet(flow, !active));
        Assert.Equal(!active, flow.LFlowWaveformActive);
        Assert.Equal([0], updates);
        Assert.Empty(TInterface.TFlowPeaksRead(flow));

        TInterface.TFlowWaveformSet(flow, active);
        TInterface.TFlowUnloadSet(flow);
        TInterface.TFlowWaveformApply(flow);
        Assert.Equal([0, 0], updates);
    }
}

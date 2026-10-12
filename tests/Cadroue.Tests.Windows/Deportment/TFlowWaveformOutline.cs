using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TFlowWaveformOutline
{
    [Fact]
    public void Outline_IsEmpty_WhenTooNarrowOrTooShortOrNoPeaks()
    {
        TimeSpan end = TimeSpan.FromSeconds(1);
        QFlowWaveformOutline narrow = TInterface.TFlowOutlineResolve([100, 200], 1, 10, 20, TimeSpan.Zero, end);
        Assert.Empty(narrow.QFlowOutlinePoints);
        Assert.Equal(0, narrow.QFlowOutlineX);
        Assert.Equal(20, narrow.QFlowOutlineY);

        QFlowWaveformOutline flat = TInterface.TFlowOutlineResolve([100, 200], 40, 10, 2, TimeSpan.Zero, end);
        Assert.Empty(flat.QFlowOutlinePoints);

        QFlowWaveformOutline silent = TInterface.TFlowOutlineResolve([], 40, 10, 20, TimeSpan.Zero, end);
        Assert.Empty(silent.QFlowOutlinePoints);
    }

    [Fact]
    public void Outline_WalksTheTopLeftToRight_ThenTheBottomBack()
    {
        byte[] peaks = new byte[25];
        Array.Fill(peaks, (byte)255);
        QFlowWaveformOutline outline = TInterface.TFlowOutlineResolve(
            peaks, 4, 0, 10, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        Assert.Equal(0, outline.QFlowOutlineX);
        Assert.Equal(1, outline.QFlowOutlineY);
        Assert.Equal(7, outline.QFlowOutlinePoints.Count);
        Assert.Equal(3, outline.QFlowOutlinePoints[2].QFlowPointX);
        Assert.Equal(1, outline.QFlowOutlinePoints[2].QFlowPointY);
        Assert.Equal(3, outline.QFlowOutlinePoints[3].QFlowPointX);
        Assert.Equal(9, outline.QFlowOutlinePoints[3].QFlowPointY);
        Assert.Equal(0, outline.QFlowOutlinePoints[6].QFlowPointX);
        Assert.Equal(9, outline.QFlowOutlinePoints[6].QFlowPointY);
    }

    [Fact]
    public void Peaks_AreEmptyWhileTheWaveformIsOff()
    {
        QFlow flow = TInterface.TFlowCreate();
        List<int> updates = [];
        TInterface.TFlowWaveformAttach(flow, () => { }, peaks => updates.Add(peaks.Length));
        bool active = flow.QFlowWaveformActive;

        Assert.False(TInterface.TFlowWaveformSet(flow, active));
        Assert.True(TInterface.TFlowWaveformSet(flow, !active));
        Assert.Equal(!active, flow.QFlowWaveformActive);
        Assert.Equal([0], updates);
        Assert.Empty(TInterface.TFlowPeaksRead(flow));

        TInterface.TFlowWaveformSet(flow, active);
        TInterface.TFlowUnloadSet(flow);
        TInterface.TFlowWaveformApply(flow);
        Assert.Equal([0, 0], updates);
    }
}

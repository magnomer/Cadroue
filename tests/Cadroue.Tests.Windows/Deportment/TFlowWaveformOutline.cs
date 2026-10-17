using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TFlowWaveformOutline
{
    [Fact]
    public void Peaks_AreEmptyWhileTheWaveformIsOff()
    {
        QFlow flow = TInterface.TFlowCreate();
        List<int> updates = [];
        TInterface.TFlowWaveformAttach(
            flow, () => { }, () => updates.Add(TInterface.TFlowWaveformCheck(flow) ? 1 : 0));
        bool active = flow.QFlowWaveformActive;

        Assert.False(TInterface.TFlowWaveformSet(flow, active));
        Assert.True(TInterface.TFlowWaveformSet(flow, !active));
        Assert.Equal(!active, flow.QFlowWaveformActive);
        Assert.Equal([0], updates);
        Assert.False(TInterface.TFlowWaveformCheck(flow));

        TInterface.TFlowWaveformSet(flow, active);
        TInterface.TFlowUnloadSet(flow);
        TInterface.TFlowWaveformApply(flow);
        Assert.Equal([0, 0], updates);
    }
}

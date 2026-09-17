using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TWhitebalanceSample
{
    [Fact]
    public void Sample_TurnsManualAndActive_WithGains()
    {
        LWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        int notices = 0;
        TInterface.TWhitebalanceAttach(whitebalance, () => notices++);

        TInterface.TWhitebalanceSampleSet(whitebalance, TInterface.TNeutralSampleCreate(120, 128, 140, 1.1, 1, 0.9));
        LWorkWhitebalanceSettings value = TInterface.TWhitebalanceValueRead(whitebalance);

        Assert.Equal(1, notices);
        Assert.True(TInterface.TWhitebalanceManualRead(whitebalance));
        Assert.True(TInterface.TWhitebalanceStepRead(whitebalance).LWorkStepActive);
        Assert.Equal(1.1, value.LWorkWhitebalanceRed);
        Assert.Equal(140, value.LWorkSampleBlue);
        Assert.True(TInterface.TWhitebalanceWheelRead(whitebalance).LNeutralWheelPresent);
    }

    [Fact]
    public void Reset_ReturnsToMedian_AndAsksForEstimate()
    {
        LWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        TInterface.TWhitebalanceSampleSet(whitebalance, TInterface.TNeutralSampleCreate(120, 128, 140, 1.1, 1, 0.9));
        var requests = new List<LWhitebalanceMethod>();
        TInterface.TWhitebalanceEstimateAttach(whitebalance, requests.Add);

        TInterface.TWhitebalanceReset(whitebalance);

        Assert.Equal(
            LWhitebalanceMethod.LWhitebalanceMethodMedian,
            TInterface.TWhitebalanceValueRead(whitebalance).LWorkWhitebalanceMethod);
        Assert.Equal(1, TInterface.TWhitebalanceValueRead(whitebalance).LWorkWhitebalanceRed);
        Assert.Equal(new[] { LWhitebalanceMethod.LWhitebalanceMethodMedian }, requests);
        Assert.False(TInterface.TWhitebalanceWheelRead(whitebalance).LNeutralWheelPresent);
    }

    [Fact]
    public void MethodChange_ToAutomatic_RequestsEstimateOnce()
    {
        LWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        var requests = new List<LWhitebalanceMethod>();
        TInterface.TWhitebalanceEstimateAttach(whitebalance, requests.Add);

        TInterface.TWhitebalanceMethodSet(whitebalance, LWhitebalanceMethod.LWhitebalanceMethodAverage);
        TInterface.TWhitebalanceMethodSet(whitebalance, LWhitebalanceMethod.LWhitebalanceMethodAverage);
        TInterface.TWhitebalanceMethodSet(whitebalance, LWhitebalanceMethod.LWhitebalanceMethodManual);

        Assert.Equal(new[] { LWhitebalanceMethod.LWhitebalanceMethodAverage }, requests);
        Assert.True(TInterface.TWhitebalanceManualRead(whitebalance));
    }

    [Fact]
    public void Picker_SwitchesTarget_DisarmsOnSample()
    {
        LWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        TInterface.TWhitebalanceCapableSet(whitebalance, true, true);

        TInterface.TWhitebalanceToolSet(whitebalance, true, LNeutralTarget.LNeutralTargetGrey);
        TInterface.TWhitebalanceToolSet(whitebalance, true, LNeutralTarget.LNeutralTargetWhite);

        Assert.True(TInterface.TWhitebalanceToolRead(whitebalance));
        Assert.Equal(LNeutralTarget.LNeutralTargetWhite, whitebalance.LWhitebalanceTarget);

        TInterface.TWhitebalanceSampleSet(whitebalance, TInterface.TNeutralSampleCreate(200, 200, 200, 1, 1, 1));

        Assert.False(TInterface.TWhitebalanceToolRead(whitebalance));
    }

    [Fact]
    public void CurveChannel_SelectAndEdit_KeepsOtherChannelsIdentity()
    {
        LCurve curve = TInterface.TCurveCreate();

        TInterface.TCurveChannelSelect(curve, 1);
        TInterface.TCurvePointAdd(curve, 0.5, 0.7);

        Assert.Equal(1, TInterface.TCurveChannelRead(curve));
        Assert.Equal(1, TInterface.TCurveSelectedRead(curve));
        Assert.Equal(3, TInterface.TCurvePointsRead(curve).Count);

        TInterface.TCurvePointSet(curve, 0.9, 0.2);
        Assert.True(TInterface.TCurvePointsRead(curve)[1].LWorkCurveInput < 1);

        LWorkCurveSettings settings = TInterface.TCurveSettingsRead(TInterface.TCurveStepRead(curve));
        Assert.Equal(2, settings.LWorkCurveMaster.Count);
        Assert.Equal(3, settings.LWorkCurveRed.Count);

        TInterface.TCurvePointDelete(curve);
        Assert.Equal(2, TInterface.TCurvePointsRead(curve).Count);
        TInterface.TCurveChannelReset(curve);
        Assert.Equal(1, TInterface.TCurveSelectedRead(curve));
    }
}

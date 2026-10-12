using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TWhitebalanceTool
{
    private static QWhitebalance TWhitebalanceCapableCreate()
    {
        QWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        TInterface.TWhitebalanceCapableSet(whitebalance, true, true);
        return whitebalance;
    }

    [Fact]
    public void Toggle_GreyThenWhite_OneArmedAtATime()
    {
        QWhitebalance whitebalance = TWhitebalanceCapableCreate();

        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetGrey, true);
        Assert.True(TInterface.TWhitebalanceGreyRead(whitebalance));
        Assert.False(TInterface.TWhitebalanceWhiteRead(whitebalance));

        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetWhite, true);
        Assert.False(TInterface.TWhitebalanceGreyRead(whitebalance));
        Assert.True(TInterface.TWhitebalanceWhiteRead(whitebalance));
    }

    [Fact]
    public void Toggle_UncheckOfOtherTarget_Ignored()
    {
        QWhitebalance whitebalance = TWhitebalanceCapableCreate();
        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetWhite, true);

        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetGrey, false);
        Assert.True(TInterface.TWhitebalanceWhiteRead(whitebalance));

        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetWhite, false);
        Assert.False(TInterface.TWhitebalanceToolRead(whitebalance));
    }

    [Fact]
    public void ToolNotice_FiresOncePerChange_WithTarget()
    {
        QWhitebalance whitebalance = TWhitebalanceCapableCreate();
        var notices = new List<(bool, LNeutralTarget)>();
        TInterface.TWhitebalanceToolAttach(whitebalance, (armed, target) => notices.Add((armed, target)));

        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetGrey, true);
        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetGrey, true);
        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetWhite, true);
        TInterface.TWhitebalanceCapableSet(whitebalance, false, false);

        Assert.Equal(
            [(true, LNeutralTarget.LNeutralTargetGrey), (true, LNeutralTarget.LNeutralTargetWhite),
                (false, LNeutralTarget.LNeutralTargetWhite)],
            notices);
    }

    [Fact]
    public void Guide_ArmedShowsGuide_DisarmedShowsStatus()
    {
        QWhitebalance whitebalance = TWhitebalanceCapableCreate();
        Assert.Equal(string.Empty, TInterface.TWhitebalanceGuideRead(whitebalance));

        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetWhite, true);
        Assert.Contains("GuideWhite", TInterface.TWhitebalanceGuideRead(whitebalance));

        TInterface.TWhitebalanceToolToggle(whitebalance, LNeutralTarget.LNeutralTargetWhite, false);
        TInterface.TWhitebalanceStatusSet(whitebalance, "picked");
        Assert.Equal("picked", TInterface.TWhitebalanceGuideRead(whitebalance));
    }

    [Fact]
    public void Readout_EmptyUntilSampled_ThenBytesAndText()
    {
        QWhitebalance whitebalance = TWhitebalanceCapableCreate();
        QWhitebalanceReadout before = TInterface.TWhitebalanceReadoutRead(whitebalance);
        Assert.False(before.QWhitebalanceReadoutSampled);
        Assert.Equal(string.Empty, before.QWhitebalanceReadoutText);

        TInterface.TWhitebalanceSampleSet(whitebalance, TInterface.TNeutralSampleCreate(120, 128, 140, 1.1, 1, 0.9));
        QWhitebalanceReadout after = TInterface.TWhitebalanceReadoutRead(whitebalance);

        Assert.True(after.QWhitebalanceReadoutSampled);
        Assert.Equal((byte)120, after.QWhitebalanceReadoutRed);
        Assert.Equal((byte)140, after.QWhitebalanceReadoutBlue);
        Assert.NotEqual(string.Empty, after.QWhitebalanceReadoutText);
    }

    [Fact]
    public void MethodSelect_MapsIndex_IgnoresNegative()
    {
        QWhitebalance whitebalance = TWhitebalanceCapableCreate();
        Assert.Equal(2, TInterface.TWhitebalanceIndexRead(whitebalance));

        TInterface.TWhitebalanceMethodSelect(whitebalance, 0);
        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodAverage, TInterface.TWhitebalanceValueRead(whitebalance)
            .LWorkWhitebalanceMethod);

        TInterface.TWhitebalanceMethodSelect(whitebalance, -1);
        Assert.Equal(0, TInterface.TWhitebalanceIndexRead(whitebalance));

        TInterface.TWhitebalanceMethodSelect(whitebalance, 3);
        Assert.True(TInterface.TWhitebalanceManualRead(whitebalance));
    }
}

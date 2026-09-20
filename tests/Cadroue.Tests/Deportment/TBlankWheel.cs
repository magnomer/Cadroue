using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TBlankWheel
{
    [Fact]
    public void FreshInstance_TypeSetColor_WheelPresent()
    {
        LBlank blank = TInterface.TBlankCreate();
        Assert.False(TInterface.TBlankPresentRead(blank));

        TInterface.TBlankTypeSet(blank, LDetectorType.LDetectorTypeColor);

        Assert.True(TInterface.TBlankPresentRead(blank));
    }

    [Fact]
    public void TypeSetBlack_AfterPick_WheelAbsent()
    {
        LBlank blank = TInterface.TBlankCreate();
        TInterface.TBlankWheelSet(blank, 0.5, 0);
        Assert.True(TInterface.TBlankPresentRead(blank));

        TInterface.TBlankTypeSet(blank, LDetectorType.LDetectorTypeBlack);

        Assert.False(TInterface.TBlankPresentRead(blank));
    }

    [Fact]
    public void StepSet_FollowsStepType()
    {
        LBlank blank = TInterface.TBlankCreate();
        TInterface.TBlankStepSet(blank, TInterface.TBlankStepCreate(LDetectorType.LDetectorTypeColor));
        Assert.True(TInterface.TBlankPresentRead(blank));

        TInterface.TBlankStepSet(blank, TInterface.TBlankStepCreate(LDetectorType.LDetectorTypeBlack));
        Assert.False(TInterface.TBlankPresentRead(blank));
    }

    [Fact]
    public void SampleSet_ForcesColorType()
    {
        LBlank blank = TInterface.TBlankCreate();

        TInterface.TBlankSampleSet(blank, 255, 0, 0);

        Assert.True(TInterface.TBlankPresentRead(blank));
        Assert.Equal(LDetectorType.LDetectorTypeColor, TInterface.TBlankStepRead(blank).LDetectorBlankType);
    }
    [Fact]
    public void DotRead_AbsentWhileBlack_PlacedAfterPress()
    {
        LBlank blank = TInterface.TBlankCreate();
        Assert.False(TInterface.TBlankDotRead(blank, 120, 11).LNeutralDotPresent);

        TInterface.TBlankWheelHandle(blank, false, 114, 60, 120);
        Assert.False(TInterface.TBlankPresentRead(blank));

        TInterface.TBlankWheelHandle(blank, true, 114, 60, 120);
        LNeutralDot dot = TInterface.TBlankDotRead(blank, 120, 11);

        Assert.True(dot.LNeutralDotPresent);
        Assert.Equal(60 + 54 - 5.5, dot.LNeutralDotLeft, 1);
        Assert.Equal(60 - 5.5, dot.LNeutralDotTop, 1);
        Assert.Equal(0, TInterface.TBlankStepRead(blank).LDetectorBlankHue, 3);
    }
}

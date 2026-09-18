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
}

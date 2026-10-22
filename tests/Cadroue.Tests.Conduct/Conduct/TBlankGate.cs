using Cadroue.Conduct;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TBlankGate
{
    [Fact]
    public void StepSet_RepeatedValue_IsSilent()
    {
        CBlank blank = new();
        int notices = 0;
        blank.CBlankChanged += () => notices++;
        LDetectorBlank step = Interface.TInterface.TDetectorBlankCreate(
            false, LDetectorType.LDetectorTypeBlack, -90, 2, 2, -1, 2, -1);

        blank.CBlankStepSet(step);
        blank.CBlankStepSet(step);

        Assert.Equal(1, notices);
        Assert.Equal(270, blank.CBlankStep.LDetectorBlankHue);
        Assert.Equal(1, blank.CBlankStep.LDetectorBlankSaturation);
        Assert.Equal(CBlank.CBlankBrightnessBound.LDetectorBoundMost, blank.CBlankStep.LDetectorBlankBrightness);
        Assert.Equal(0, blank.CBlankStep.LDetectorBlankTolerance);
        Assert.Equal(1, blank.CBlankStep.LDetectorBlankCoverage);
        Assert.Equal(CBlank.CBlankMinimumBound.LDetectorBoundLeast, blank.CBlankStep.LDetectorBlankMinimum);
    }

    [Fact]
    public void WheelSet_ClampsAndRaisesOnce()
    {
        CBlank blank = new();
        int notices = 0;
        blank.CBlankChanged += () => notices++;

        blank.CBlankWheelSet(3, 4);
        blank.CBlankWheelSet(6, 8);

        Assert.Equal(1, notices);
        Assert.Equal(0.6, blank.CBlankWheelX, 6);
        Assert.Equal(0.8, blank.CBlankWheelY, 6);
        Assert.Equal(1, blank.CBlankStep.LDetectorBlankSaturation);
        Assert.Equal(LDetectorType.LDetectorTypeColor, blank.CBlankStep.LDetectorBlankType);
    }

    [Fact]
    public void SampleSet_ForcesColorAndRaisesOnce()
    {
        CBlank blank = new();
        int notices = 0;
        blank.CBlankChanged += () => notices++;

        blank.CBlankSampleSet(0, 128, 0);
        blank.CBlankSampleSet(0, 128, 0);

        Assert.Equal(1, notices);
        Assert.Equal(LDetectorType.LDetectorTypeColor, blank.CBlankStep.LDetectorBlankType);
        Assert.Equal(120, blank.CBlankStep.LDetectorBlankHue, 6);
        Assert.Equal(1, blank.CBlankStep.LDetectorBlankSaturation, 6);
        Assert.Equal(128.0 / 255.0, blank.CBlankStep.LDetectorBlankBrightness, 6);
    }
}

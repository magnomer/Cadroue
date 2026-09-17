using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LBlank
{
    private LDetectorBlank lBlankStep = LDetectorBlank.LDetectorBlankCreate();
    private bool lBlankWheelPresent;
    private bool lBlankPicking;

    public event Action? LBlankChange;
    public event Action<bool>? LBlankPickChange;

    public LDetectorBlank LBlankStep => lBlankStep;

    public bool LBlankWheelPresent => lBlankWheelPresent;

    public bool LBlankPicking => lBlankPicking;

    public double LBlankWheelX =>
        lBlankStep.LDetectorBlankSaturation * Math.Cos(lBlankStep.LDetectorBlankHue * (Math.PI / 180.0));

    public double LBlankWheelY =>
        lBlankStep.LDetectorBlankSaturation * Math.Sin(lBlankStep.LDetectorBlankHue * (Math.PI / 180.0));

    public void LBlankStepSet(LDetectorBlank lStep)
    {
        LDetectorBlank lNormal = LDetectorBlank.LDetectorBlankClamp(lStep);
        bool lPresent = lNormal.LDetectorBlankType == LDetectorType.LDetectorTypeColor;
        if (lBlankStep == lNormal && lBlankWheelPresent == lPresent)
        {
            return;
        }

        lBlankStep = lNormal;
        lBlankWheelPresent = lPresent;
        LBlankChange?.Invoke();
    }

    public void LBlankEnabledSet(bool lEnabled) =>
        LBlankStepApply(lBlankStep with { LDetectorBlankEnabled = lEnabled });

    public void LBlankTypeSet(LDetectorType lType) =>
        LBlankStepApply(lBlankStep with { LDetectorBlankType = lType });

    public void LBlankBrightnessSet(double lBrightness) =>
        LBlankStepApply(lBlankStep with { LDetectorBlankBrightness = lBrightness });

    public void LBlankToleranceSet(double lTolerance) =>
        LBlankStepApply(lBlankStep with { LDetectorBlankTolerance = lTolerance });

    public void LBlankCoverageSet(double lCoverage) =>
        LBlankStepApply(lBlankStep with { LDetectorBlankCoverage = lCoverage });

    public void LBlankMinimumSet(double lMinimum) =>
        LBlankStepApply(lBlankStep with { LDetectorBlankMinimum = lMinimum });

    public void LBlankWheelSet(double lX, double lY)
    {
        double lReach = Math.Sqrt((lX * lX) + (lY * lY));
        if (lReach > 1)
        {
            lX /= lReach;
            lY /= lReach;
        }

        double lBrightness = lBlankStep.LDetectorBlankBrightness <= 0.0001
            ? LDetectorBlank.LDetectorBlankValue
            : lBlankStep.LDetectorBlankBrightness;
        LBlankColorApply(lX, lY, lBrightness);
    }

    public void LBlankSampleSet(int lRed, int lGreen, int lBlue)
    {
        LBlankPickSet(false);
        LNeutralWheel lWheel = LNeutral.LNeutralWheelResolve(lRed, lGreen, lBlue);
        LBlankColorApply(lWheel.LNeutralWheelX, lWheel.LNeutralWheelY, Math.Max(lRed, Math.Max(lGreen, lBlue)) / 255.0);
    }

    public void LBlankPickSet(bool lPicking)
    {
        if (lBlankPicking == lPicking)
        {
            return;
        }

        lBlankPicking = lPicking;
        LBlankPickChange?.Invoke(lPicking);
    }

    private void LBlankColorApply(double lX, double lY, double lBrightness)
    {
        double lHue = Math.Atan2(lY, lX) * (180.0 / Math.PI);
        if (lHue < 0)
        {
            lHue += 360;
        }

        lBlankWheelPresent = true;
        LDetectorBlank lNormal = LDetectorBlank.LDetectorBlankClamp(lBlankStep with
        {
            LDetectorBlankType = LDetectorType.LDetectorTypeColor,
            LDetectorBlankHue = lHue,
            LDetectorBlankSaturation = Math.Clamp(Math.Sqrt((lX * lX) + (lY * lY)), 0, 1),
            LDetectorBlankBrightness = lBrightness
        });
        lBlankStep = lNormal;
        LBlankChange?.Invoke();
    }

    private void LBlankStepApply(LDetectorBlank lStep)
    {
        LDetectorBlank lNormal = LDetectorBlank.LDetectorBlankClamp(lStep);
        if (lBlankStep == lNormal)
        {
            return;
        }

        lBlankStep = lNormal;
        LBlankChange?.Invoke();
    }
}

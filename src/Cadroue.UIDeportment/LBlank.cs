using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LBlank
{
    private LDetectorBlank lBlankStep = LDetectorBlank.LDetectorBlankCreate();
    private bool lBlankPicking;

    public event Action? LBlankChange;
    public event Action<bool>? LBlankPickChange;

    public LDetectorBlank LBlankStep => lBlankStep;

    public LDetectorKind LBlankKind => LDetectorKind.LDetectorKindBlank;

    public bool LBlankWheelPresent => lBlankStep.LDetectorBlankType == LDetectorType.LDetectorTypeColor;

    public bool LBlankPicking => lBlankPicking;

    public static LDetectorBound LBlankBrightnessBound =>
        LDetector.LDetectorBrightnessRead() with { LDetectorBoundDefault = LDetectorBlank.LDetectorBlankValue };

    public static LDetectorBound LBlankMinimumBound =>
        LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindBlank)
            with { LDetectorBoundDefault = LDetectorBlank.LDetectorBlankGap };

    public double LBlankWheelX =>
        lBlankStep.LDetectorBlankSaturation * Math.Cos(lBlankStep.LDetectorBlankHue * (Math.PI / 180.0));

    public double LBlankWheelY =>
        lBlankStep.LDetectorBlankSaturation * Math.Sin(lBlankStep.LDetectorBlankHue * (Math.PI / 180.0));

    public void LBlankStepSet(LDetectorBlank lStep)
    {
        LDetectorBlank lNormal = LDetectorBlank.LDetectorBlankClamp(lStep);
        if (lBlankStep == lNormal)
        {
            return;
        }

        lBlankStep = lNormal;
        LBlankChange?.Invoke();
    }

    public void LBlankEnabledSet(bool lEnabled) =>
        LBlankStepSet(lBlankStep with { LDetectorBlankEnabled = lEnabled });

    public void LBlankTypeSet(LDetectorType lType) =>
        LBlankStepSet(lBlankStep with { LDetectorBlankType = lType });

    public void LBlankBrightnessSet(double lBrightness) =>
        LBlankStepSet(lBlankStep with { LDetectorBlankBrightness = lBrightness });

    public void LBlankToleranceSet(double lTolerance) =>
        LBlankStepSet(lBlankStep with { LDetectorBlankTolerance = lTolerance });

    public void LBlankCoverageSet(double lCoverage) =>
        LBlankStepSet(lBlankStep with { LDetectorBlankCoverage = lCoverage });

    public void LBlankMinimumSet(double lMinimum) =>
        LBlankStepSet(lBlankStep with { LDetectorBlankMinimum = lMinimum });

    public LNeutralDot LBlankDotRead(double lSize, double lDot) =>
        LNeutral.LNeutralDotResolve(new LNeutralWheel(LBlankWheelX, LBlankWheelY, LBlankWheelPresent), lSize, lDot);

    public void LBlankWheelHandle(bool lPressed, double lX, double lY, double lSize)
    {
        if (!lPressed)
        {
            return;
        }

        LNeutralWheel lWheel = LNeutral.LNeutralCanvasResolve(lX, lY, lSize);
        LBlankWheelSet(lWheel.LNeutralWheelX, lWheel.LNeutralWheelY);
    }

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

        LBlankStepSet(lBlankStep with
        {
            LDetectorBlankType = LDetectorType.LDetectorTypeColor,
            LDetectorBlankHue = lHue,
            LDetectorBlankSaturation = Math.Clamp(Math.Sqrt((lX * lX) + (lY * lY)), 0, 1),
            LDetectorBlankBrightness = lBrightness
        });
    }
}

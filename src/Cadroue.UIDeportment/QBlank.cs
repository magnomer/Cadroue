using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QBlank
{
    private LDetectorBlank qBlankStep = LDetectorBlank.LDetectorBlankCreate();
    private bool qBlankPicking;

    public event Action? QBlankChanged;
    public event Action<bool>? QBlankPickChanged;

    public LDetectorBlank QBlankStep => qBlankStep;

    public LDetectorKind QBlankKind => LDetectorKind.LDetectorKindBlank;

    public bool QBlankWheelPresent => qBlankStep.LDetectorBlankType == LDetectorType.LDetectorTypeColor;

    public bool QBlankPicking => qBlankPicking;

    public static LDetectorBound QBlankBrightnessBound =>
        LDetector.LDetectorBrightnessRead() with { LDetectorBoundDefault = LDetectorBlank.LDetectorBlankValue };

    public static LDetectorBound QBlankMinimumBound =>
        LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindBlank)
            with { LDetectorBoundDefault = LDetectorBlank.LDetectorBlankGap };

    public double QBlankWheelX =>
        qBlankStep.LDetectorBlankSaturation * Math.Cos(qBlankStep.LDetectorBlankHue * (Math.PI / 180.0));

    public double QBlankWheelY =>
        qBlankStep.LDetectorBlankSaturation * Math.Sin(qBlankStep.LDetectorBlankHue * (Math.PI / 180.0));

    public void QBlankStepSet(LDetectorBlank lStep)
    {
        LDetectorBlank lNormal = LDetectorBlank.LDetectorBlankClamp(lStep);
        if (qBlankStep == lNormal)
        {
            return;
        }

        qBlankStep = lNormal;
        QBlankChanged?.Invoke();
    }

    public void QBlankEnabledSet(bool lEnabled) =>
        QBlankStepSet(qBlankStep with { LDetectorBlankEnabled = lEnabled });

    public void QBlankTypeSet(LDetectorType lType) =>
        QBlankStepSet(qBlankStep with { LDetectorBlankType = lType });

    public void QBlankBrightnessSet(double lBrightness) =>
        QBlankStepSet(qBlankStep with { LDetectorBlankBrightness = lBrightness });

    public void QBlankToleranceSet(double lTolerance) =>
        QBlankStepSet(qBlankStep with { LDetectorBlankTolerance = lTolerance });

    public void QBlankCoverageSet(double lCoverage) =>
        QBlankStepSet(qBlankStep with { LDetectorBlankCoverage = lCoverage });

    public void QBlankMinimumSet(double lMinimum) =>
        QBlankStepSet(qBlankStep with { LDetectorBlankMinimum = lMinimum });

    public LNeutralDot QBlankDotRead(double lSize, double lDot) =>
        LNeutralCanvas.LNeutralDotResolve(
            new LNeutralWheel(QBlankWheelX, QBlankWheelY, QBlankWheelPresent), lSize, lDot);

    public void QBlankWheelHandle(bool lPressed, double lX, double lY, double lSize)
    {
        if (!lPressed)
        {
            return;
        }

        LNeutralWheel lWheel = LNeutralCanvas.LNeutralCanvasResolve(lX, lY, lSize);
        QBlankWheelSet(lWheel.LNeutralWheelX, lWheel.LNeutralWheelY);
    }

    public void QBlankWheelSet(double lX, double lY)
    {
        (double lWheelX, double lWheelY) = QBlankWheelClamp(lX, lY);
        double lBrightness = QBlankBrightnessResolve();
        QBlankColorApply(lWheelX, lWheelY, lBrightness);
    }

    public void QBlankSampleSet(int lRed, int lGreen, int lBlue)
    {
        QBlankPickSet(false);
        LNeutralWheel lWheel = LNeutralColor.LNeutralWheelResolve(lRed, lGreen, lBlue);
        QBlankColorApply(lWheel.LNeutralWheelX, lWheel.LNeutralWheelY, QBlankValueResolve(lRed, lGreen, lBlue));
    }

    // A point outside the wheel is pulled back onto its rim.
    private static (double QBlankWheelX, double QBlankWheelY) QBlankWheelClamp(double lX, double lY)
    {
        double lReach = Math.Sqrt((lX * lX) + (lY * lY));
        if (lReach > 1)
        {
            lX /= lReach;
            lY /= lReach;
        }

        return (lX, lY);
    }

    private double QBlankBrightnessResolve() =>
        qBlankStep.LDetectorBlankBrightness <= 0.0001
            ? LDetectorBlank.LDetectorBlankValue
            : qBlankStep.LDetectorBlankBrightness;

    private static double QBlankValueResolve(int lRed, int lGreen, int lBlue) =>
        Math.Max(lRed, Math.Max(lGreen, lBlue)) / 255.0;

    public void QBlankPickSet(bool lPicking)
    {
        if (qBlankPicking == lPicking)
        {
            return;
        }

        qBlankPicking = lPicking;
        QBlankPickChanged?.Invoke(lPicking);
    }

    private void QBlankColorApply(double lX, double lY, double lBrightness) =>
        QBlankStepSet(QBlankColorCreate(lX, lY, lBrightness));

    private LDetectorBlank QBlankColorCreate(double lX, double lY, double lBrightness)
    {
        double lHue = Math.Atan2(lY, lX) * (180.0 / Math.PI);
        if (lHue < 0)
        {
            lHue += 360;
        }

        return qBlankStep with
        {
            LDetectorBlankType = LDetectorType.LDetectorTypeColor,
            LDetectorBlankHue = lHue,
            LDetectorBlankSaturation = Math.Clamp(Math.Sqrt((lX * lX) + (lY * lY)), 0, 1),
            LDetectorBlankBrightness = lBrightness
        };
    }
}

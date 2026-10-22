using System;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Conduct;

// The medium-free decisions of a blank detector: normalized state, effective changes, and color-wheel values.
public sealed class CBlank
{
    private LDetectorBlank cBlankStep = LDetectorBlank.LDetectorBlankCreate();

    public event Action? CBlankChanged;

    public LDetectorBlank CBlankStep => cBlankStep;

    public bool CBlankWheelPresent => cBlankStep.LDetectorBlankType == LDetectorType.LDetectorTypeColor;

    public static LDetectorBound CBlankBrightnessBound =>
        LDetector.LDetectorBrightnessRead() with { LDetectorBoundDefault = LDetectorBlank.LDetectorBlankValue };

    public static LDetectorBound CBlankMinimumBound =>
        LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindBlank)
            with { LDetectorBoundDefault = LDetectorBlank.LDetectorBlankGap };

    public double CBlankWheelX =>
        cBlankStep.LDetectorBlankSaturation * Math.Cos(cBlankStep.LDetectorBlankHue * (Math.PI / 180.0));

    public double CBlankWheelY =>
        cBlankStep.LDetectorBlankSaturation * Math.Sin(cBlankStep.LDetectorBlankHue * (Math.PI / 180.0));

    public void CBlankStepSet(LDetectorBlank step)
    {
        LDetectorBlank normal = LDetectorBlank.LDetectorBlankClamp(step);
        if (cBlankStep == normal)
        {
            return;
        }

        cBlankStep = normal;
        CBlankChanged?.Invoke();
    }

    public void CBlankEnabledSet(bool enabled) =>
        CBlankStepSet(cBlankStep with { LDetectorBlankEnabled = enabled });

    public void CBlankTypeSet(LDetectorType type) =>
        CBlankStepSet(cBlankStep with { LDetectorBlankType = type });

    public void CBlankBrightnessSet(double brightness) =>
        CBlankStepSet(cBlankStep with { LDetectorBlankBrightness = brightness });

    public void CBlankToleranceSet(double tolerance) =>
        CBlankStepSet(cBlankStep with { LDetectorBlankTolerance = tolerance });

    public void CBlankCoverageSet(double coverage) =>
        CBlankStepSet(cBlankStep with { LDetectorBlankCoverage = coverage });

    public void CBlankMinimumSet(double minimum) =>
        CBlankStepSet(cBlankStep with { LDetectorBlankMinimum = minimum });

    public LNeutralDot CBlankDotRead(double size, double dot) =>
        LNeutralCanvas.LNeutralDotResolve(
            new LNeutralWheel(CBlankWheelX, CBlankWheelY, CBlankWheelPresent), size, dot);

    public void CBlankWheelSet(double x, double y)
    {
        (double wheelX, double wheelY) = CBlankWheelClamp(x, y);
        CBlankColorApply(wheelX, wheelY, CBlankBrightnessResolve());
    }

    public void CBlankWheelSet(double x, double y, double size)
    {
        LNeutralWheel wheel = LNeutralCanvas.LNeutralCanvasResolve(x, y, size);
        CBlankWheelSet(wheel.LNeutralWheelX, wheel.LNeutralWheelY);
    }

    public void CBlankSampleSet(int red, int green, int blue)
    {
        LNeutralWheel wheel = LNeutralColor.LNeutralWheelResolve(red, green, blue);
        CBlankColorApply(wheel.LNeutralWheelX, wheel.LNeutralWheelY, CBlankValueResolve(red, green, blue));
    }

    private static (double CBlankWheelX, double CBlankWheelY) CBlankWheelClamp(double x, double y)
    {
        double reach = Math.Sqrt((x * x) + (y * y));
        if (reach > 1)
        {
            x /= reach;
            y /= reach;
        }

        return (x, y);
    }

    private double CBlankBrightnessResolve() =>
        cBlankStep.LDetectorBlankBrightness <= 0.0001
            ? LDetectorBlank.LDetectorBlankValue
            : cBlankStep.LDetectorBlankBrightness;

    private static double CBlankValueResolve(int red, int green, int blue) =>
        Math.Max(red, Math.Max(green, blue)) / 255.0;

    private void CBlankColorApply(double x, double y, double brightness) =>
        CBlankStepSet(CBlankColorCreate(x, y, brightness));

    private LDetectorBlank CBlankColorCreate(double x, double y, double brightness)
    {
        double hue = Math.Atan2(y, x) * (180.0 / Math.PI);
        if (hue < 0)
        {
            hue += 360;
        }

        return cBlankStep with
        {
            LDetectorBlankType = LDetectorType.LDetectorTypeColor,
            LDetectorBlankHue = hue,
            LDetectorBlankSaturation = Math.Clamp(Math.Sqrt((x * x) + (y * y)), 0, 1),
            LDetectorBlankBrightness = brightness
        };
    }
}

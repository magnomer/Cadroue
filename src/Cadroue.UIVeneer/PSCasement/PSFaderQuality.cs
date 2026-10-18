using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIVeneer;

internal static class PSFaderQuality
{
    internal static UIElement PSFaderQualityBuild(
        double pMinimum,
        double pMaximum,
        double pStep,
        string pValue,
        TextBox pReadout,
        bool pHigherBetter)
    {
        double pStart = PSFaderQualityParse(pValue, pMinimum, pMinimum, pMaximum, pStep);
        Slider pSlider = PSFader.PSFaderStepBuild(
            pMinimum, pMaximum, pStep, PSFaderQualityResolve(pStart, pHigherBetter, pMinimum, pMaximum));
        PSFader.PSFaderReadoutSet(pReadout, PSFaderValueFormat(pStart, pStep));
        var pSync = new StrongBox<bool>(false);
        PSFaderQualityAttach(pSlider, pReadout, pSync, pMinimum, pMaximum, pStep, pHigherBetter);
        return PSFader.PSFaderRowBuild(pSlider, pReadout);
    }

    private static double PSFaderQualityParse(
        string pText,
        double pFallback,
        double pMinimum,
        double pMaximum,
        double pStep) =>
        double.TryParse(pText, NumberStyles.Float, CultureInfo.InvariantCulture, out double pParsed)
            && double.IsFinite(pParsed)
            ? PSFaderQualityNormalize(pParsed, pMinimum, pMaximum, pStep)
            : pFallback;

    private static double PSFaderQualityResolve(
        double pPosition,
        bool pHigherBetter,
        double pMinimum,
        double pMaximum) =>
        pHigherBetter ? pPosition : pMinimum + pMaximum - pPosition;

    private static void PSFaderQualityAttach(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        double pMinimum,
        double pMaximum,
        double pStep,
        bool pHigherBetter)
    {
        pSlider.ValueChanged += (_, _) =>
            PSFaderQualityHandle(pSlider, pReadout, pSync, pMinimum, pMaximum, pStep, pHigherBetter);
        pReadout.KeyDown += (_, pEvent) => PSFader.PSFaderReturnHandle(
            pEvent,
            () => PSFaderQualityCommit(pSlider, pReadout, pSync, pMinimum, pMaximum, pStep, pHigherBetter));
        pReadout.LostKeyboardFocus += (_, _) =>
            PSFaderQualityCommit(pSlider, pReadout, pSync, pMinimum, pMaximum, pStep, pHigherBetter);
    }

    private static void PSFaderQualityHandle(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        double pMinimum,
        double pMaximum,
        double pStep,
        bool pHigherBetter)
    {
        if (pSync.Value)
        {
            return;
        }

        PSFader.PSFaderReadoutSet(
            pReadout,
            PSFaderValueFormat(PSFaderQualityResolve(pSlider.Value, pHigherBetter, pMinimum, pMaximum), pStep));
    }

    private static void PSFaderQualityCommit(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        double pMinimum,
        double pMaximum,
        double pStep,
        bool pHigherBetter)
    {
        double pQuality = PSFaderQualityParse(
            pReadout.Text.Trim(),
            PSFaderQualityResolve(pSlider.Value, pHigherBetter, pMinimum, pMaximum),
            pMinimum,
            pMaximum,
            pStep);
        PSFader.PSFaderSyncSet(pSync, true);
        PSFader.PSFaderPairSet(
            pSlider,
            pReadout,
            PSFaderQualityResolve(pQuality, pHigherBetter, pMinimum, pMaximum),
            PSFaderValueFormat(pQuality, pStep));
        PSFader.PSFaderSyncSet(pSync, false);
    }

    private static string PSFaderValueFormat(double pValue, double pStep) =>
        pStep >= 1 && pValue == Math.Floor(pValue)
            ? ((long)Math.Round(pValue)).ToString(CultureInfo.InvariantCulture)
            : pValue.ToString("0.##", CultureInfo.InvariantCulture);

    private static double PSFaderQualityNormalize(double pValue, double pMinimum, double pMaximum, double pStep)
    {
        double pClamped = Math.Clamp(pValue, pMinimum, pMaximum);
        double pStepped = pMinimum + Math.Round((pClamped - pMinimum) / pStep, MidpointRounding.AwayFromZero) * pStep;
        return Math.Clamp(pStepped, pMinimum, pMaximum);
    }
}

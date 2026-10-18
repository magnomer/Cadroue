using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSFaderDetent
{
    internal static UIElement PSFaderDetentBuild(
        IReadOnlyList<int> pRates,
        bool pSnap,
        double pMaximum,
        string pZeroLabel,
        string pValue,
        TextBox pReadout,
        UIElement? pNotice = null)
    {
        double pMax = Math.Max(pMaximum, 1);
        double pStart = PSFaderHzParse(pValue, pRates, pSnap);
        DoubleCollection pTicks = PSFaderTicksCreate(pRates);
        Slider pSlider = PSFaderRateBuild(pMax, pTicks, pSnap, Math.Min(pStart, pMax));
        PSFader.PSFaderEditApply(pReadout, pSnap);
        PSFader.PSFaderReadoutSet(pReadout, PSFaderDetentFormat(pStart, pZeroLabel));
        PSFader.PSFaderNoticeSet(pNotice, PSFader.PSFaderNoticeResolve(pStart));
        var pSync = new StrongBox<bool>(false);
        PSFaderDetentAttach(pSlider, pReadout, pSync, pRates, pSnap, pMax, pZeroLabel, pNotice);
        return PSFader.PSFaderRowBuild(pSlider, pReadout);
    }

    private static double PSFaderHzParse(string pValue, IReadOnlyList<int> pRates, bool pSnap) =>
        int.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pParsed)
            && pParsed > 0
                ? PSFaderDetentResolve(pParsed, pRates, pSnap)
                : 0;

    private static DoubleCollection PSFaderTicksCreate(IReadOnlyList<int> pRates)
    {
        var pTicks = new DoubleCollection { 0 };
        foreach (int pRate in pRates)
        {
            pTicks.Add(pRate);
        }

        return pTicks;
    }

    private static Slider PSFaderRateBuild(double pMax, DoubleCollection pTicks, bool pSnap, double pPosition)
    {
        var pSlider = new Slider
        {
            Minimum = 0,
            Maximum = pMax,
            SmallChange = 1,
            LargeChange = 1,
            Ticks = pTicks,
            TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
            IsSnapToTickEnabled = pSnap,
            Value = pPosition,
            Width = PSFader.PSFaderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        QSlider.QSliderAttach(pSlider);
        pSlider.IsSnapToTickEnabled = true;
        return pSlider;
    }

    private static void PSFaderDetentAttach(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        IReadOnlyList<int> pRates,
        bool pSnap,
        double pMax,
        string pZeroLabel,
        UIElement? pNotice)
    {
        pSlider.ValueChanged += (_, _) => PSFaderDetentHandle(pSlider, pReadout, pSync, pZeroLabel, pNotice);
        pReadout.KeyDown += (_, pEvent) => PSFader.PSFaderReturnHandle(
            pEvent,
            () => PSFaderDetentCommit(pSlider, pReadout, pSync, pRates, pSnap, pMax, pZeroLabel, pNotice));
        pReadout.LostKeyboardFocus += (_, _) =>
            PSFaderDetentCommit(pSlider, pReadout, pSync, pRates, pSnap, pMax, pZeroLabel, pNotice);
    }

    private static void PSFaderDetentHandle(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        string pZeroLabel,
        UIElement? pNotice)
    {
        if (!pSync.Value)
        {
            PSFader.PSFaderReadoutSet(pReadout, PSFaderDetentFormat(pSlider.Value, pZeroLabel));
        }

        PSFader.PSFaderNoticeSet(pNotice, PSFader.PSFaderNoticeResolve(pSlider.Value));
    }

    private static void PSFaderDetentCommit(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        IReadOnlyList<int> pRates,
        bool pSnap,
        double pMax,
        string pZeroLabel,
        UIElement? pNotice)
    {
        double pResolved = PSFaderDetentParse(pReadout.Text.Trim(), pZeroLabel, pSlider.Value, pRates, pSnap);
        PSFader.PSFaderSyncSet(pSync, true);
        PSFader.PSFaderPairSet(
            pSlider, pReadout, Math.Min(pResolved, pMax), PSFaderDetentFormat(pResolved, pZeroLabel));
        PSFader.PSFaderSyncSet(pSync, false);
        PSFader.PSFaderNoticeSet(pNotice, PSFader.PSFaderNoticeResolve(pResolved));
    }

    private static double PSFaderDetentParse(
        string pTyped,
        string pZeroLabel,
        double pFallback,
        IReadOnlyList<int> pRates,
        bool pSnap)
    {
        if (pTyped.Length == 0 || string.Equals(pTyped, pZeroLabel, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (int.TryParse(pTyped, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pHz) && pHz > 0)
        {
            return PSFaderDetentResolve(pHz, pRates, pSnap);
        }

        return pFallback;
    }

    private static double PSFaderDetentResolve(double pHz, IReadOnlyList<int> pRates, bool pSnap)
    {
        if (pHz <= 0 || !pSnap || pRates.Count == 0)
        {
            return pHz <= 0 ? 0 : pHz;
        }

        double pBest = pRates[0];
        double pBestGap = double.MaxValue;
        foreach (int pRate in pRates)
        {
            double pGap = Math.Abs(pRate - pHz);
            if (pGap < pBestGap)
            {
                pBestGap = pGap;
                pBest = pRate;
            }
        }

        return pBest;
    }

    private static string PSFaderDetentFormat(double pValue, string pZeroLabel) =>
        pValue <= 0 ? pZeroLabel : ((long)Math.Round(pValue)).ToString(CultureInfo.InvariantCulture);
}

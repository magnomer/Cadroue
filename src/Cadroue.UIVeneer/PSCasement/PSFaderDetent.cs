using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PSCasement;

internal static partial class PSFader
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
        double pStart = int.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pParsed)
            && pParsed > 0
                ? PSFaderDetentResolve(pParsed, pRates, pSnap)
                : 0;

        var pTicks = new DoubleCollection { 0 };
        foreach (int pRate in pRates)
        {
            pTicks.Add(pRate);
        }

        var pSlider = new Slider
        {
            Minimum = 0,
            Maximum = pMax,
            SmallChange = 1,
            LargeChange = 1,
            Ticks = pTicks,
            TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
            IsSnapToTickEnabled = pSnap,
            Value = Math.Min(pStart, pMax),
            Width = PSFaderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        pSlider.IsSnapToTickEnabled = true;
        pReadout.IsReadOnly = pSnap;
        pReadout.Text = PSFaderDetentFormat(pStart, pZeroLabel);

        void PSFaderDetentApply(double pAt)
        {
            if (pNotice is not null)
            {
                pNotice.Visibility = pAt <= 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        PSFaderDetentApply(pStart);

        bool pSync = false;
        void PSFaderDetentCommit()
        {
            string pTyped = pReadout.Text.Trim();
            double pResolved;
            if (pTyped.Length == 0 || string.Equals(pTyped, pZeroLabel, StringComparison.OrdinalIgnoreCase))
            {
                pResolved = 0;
            }
            else if (int.TryParse(pTyped, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pHz) && pHz > 0)
            {
                pResolved = PSFaderDetentResolve(pHz, pRates, pSnap);
            }
            else
            {
                pResolved = pSlider.Value;
            }

            pSync = true;
            pSlider.Value = Math.Min(pResolved, pMax);
            pReadout.Text = PSFaderDetentFormat(pResolved, pZeroLabel);
            pReadout.CaretIndex = pReadout.Text.Length;
            pSync = false;
            PSFaderDetentApply(pResolved);
        }

        pSlider.ValueChanged += (_, _) =>
        {
            if (!pSync)
            {
                pReadout.Text = PSFaderDetentFormat(pSlider.Value, pZeroLabel);
            }

            PSFaderDetentApply(pSlider.Value);
        };
        pReadout.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PSFaderDetentCommit();
                pEvent.Handled = true;
            }
        };
        pReadout.LostKeyboardFocus += (_, _) => PSFaderDetentCommit();
        return PSFaderRowBuild(pSlider, pReadout);
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

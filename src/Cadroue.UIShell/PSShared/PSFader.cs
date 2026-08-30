using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal static class PSFader
{
    private const double PSFaderWidth = 220;
    private const double PSFaderBitrateTicks = 1000;

    internal static UIElement PSFaderQualityBuild(double pMinimum, double pMaximum, double pStep, string pValue, TextBox pReadout, bool pHigherBetter)
    {
        double pStart = double.TryParse(pValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double pParsed)
            && double.IsFinite(pParsed)
            ? PSFaderQualityNormalize(pParsed, pMinimum, pMaximum, pStep)
            : pMinimum;

        double PSFaderQualityResolve(double pPosition) => pHigherBetter ? pPosition : pMinimum + pMaximum - pPosition;

        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            SmallChange = pStep,
            LargeChange = pStep,
            TickFrequency = pStep,
            IsSnapToTickEnabled = true,
            Value = PSFaderQualityResolve(pStart),
            Width = PSFaderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);

        pReadout.Text = PSFaderValueFormat(pStart, pStep);

        bool pSync = false;
        void PSFaderQualityCommit()
        {
            double pQuality;
            if (!double.TryParse(pReadout.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double pTyped)
                || !double.IsFinite(pTyped))
            {
                pQuality = PSFaderQualityResolve(pSlider.Value);
            }
            else
            {
                pQuality = PSFaderQualityNormalize(pTyped, pMinimum, pMaximum, pStep);
            }

            pSync = true;
            pSlider.Value = PSFaderQualityResolve(pQuality);
            pReadout.Text = PSFaderValueFormat(pQuality, pStep);
            pReadout.CaretIndex = pReadout.Text.Length;
            pSync = false;
        }

        pSlider.ValueChanged += (_, _) =>
        {
            if (!pSync)
            {
                pReadout.Text = PSFaderValueFormat(PSFaderQualityResolve(pSlider.Value), pStep);
            }
        };
        pReadout.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PSFaderQualityCommit();
                pEvent.Handled = true;
            }
        };
        pReadout.LostKeyboardFocus += (_, _) => PSFaderQualityCommit();
        return PSFaderRowBuild(pSlider, pReadout);
    }

    internal static UIElement PSFaderBitrateBuild(double pMinimumKbps, double pMaximumKbps, string pValue, TextBox pReadout)
    {
        double pStartKbps = Math.Clamp(PSFaderBitrateParse(pValue) ?? pMinimumKbps, pMinimumKbps, pMaximumKbps);

        var pSlider = new Slider
        {
            Minimum = 0,
            Maximum = PSFaderBitrateTicks,
            SmallChange = 1,
            LargeChange = 50,
            Value = PSFaderPositionResolve(pStartKbps, pMinimumKbps, pMaximumKbps),
            Width = PSFaderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        pReadout.Text = PSFaderBitrateFormat(pStartKbps);

        bool pSync = false;
        pSlider.ValueChanged += (_, _) =>
        {
            if (pSync)
            {
                return;
            }

            pSync = true;
            pReadout.Text = PSFaderBitrateFormat(PSFaderBitrateResolve(pSlider.Value, pMinimumKbps, pMaximumKbps));
            pSync = false;
        };
        pReadout.TextChanged += (_, _) =>
        {
            if (pSync || PSFaderBitrateParse(pReadout.Text) is not double pKbps)
            {
                return;
            }

            pSync = true;
            pSlider.Value = PSFaderPositionResolve(Math.Clamp(pKbps, pMinimumKbps, pMaximumKbps), pMinimumKbps, pMaximumKbps);
            pSync = false;
        };
        return PSFaderRowBuild(pSlider, pReadout);
    }

    internal static UIElement PSFaderDetentBuild(IReadOnlyList<int> pRates, bool pSnap, double pMaximum, string pZeroLabel, string pValue, TextBox pReadout, UIElement? pNotice = null)
    {
        double pMax = Math.Max(pMaximum, 1);
        double pStart = int.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pParsed) && pParsed > 0
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

    internal static UIElement PSFaderLayoutBuild(Slider pSlider, IReadOnlyList<string> pLabels, int pIndex, TextBox pReadout, UIElement? pNotice = null)
    {
        int pLast = Math.Max(pLabels.Count - 1, 0);
        int pStart = Math.Clamp(pIndex, 0, pLast);

        var pTicks = new DoubleCollection();
        for (int pTick = 0; pTick <= pLast; pTick++)
        {
            pTicks.Add(pTick);
        }

        pSlider.Minimum = 0;
        pSlider.Maximum = pLast;
        pSlider.SmallChange = 1;
        pSlider.LargeChange = 1;
        pSlider.Ticks = pTicks;
        pSlider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight;
        pSlider.IsSnapToTickEnabled = true;
        pSlider.Value = pStart;
        pSlider.Width = PSFaderWidth;
        pSlider.VerticalAlignment = VerticalAlignment.Center;
        PSlider.PSliderApply(pSlider);
        pSlider.IsSnapToTickEnabled = true;

        pReadout.IsReadOnly = true;
        pReadout.Text = pLabels.Count > 0 ? pLabels[pStart] : string.Empty;

        if (pNotice is not null)
        {
            pNotice.Visibility = pStart <= 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        pSlider.ValueChanged += (_, _) =>
        {
            int pAt = Math.Clamp((int)Math.Round(pSlider.Value), 0, pLast);
            pReadout.Text = pLabels.Count > 0 ? pLabels[pAt] : string.Empty;
            if (pNotice is not null)
            {
                pNotice.Visibility = pAt <= 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        };

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

    internal static UIElement PSFaderRowBuild(Slider pSlider, TextBox pReadout)
    {
        pReadout.Width = 88;
        pReadout.Margin = new Thickness(12, 0, 0, 0);
        pReadout.VerticalAlignment = VerticalAlignment.Center;
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = PSField.PSFieldControlHeight
        };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pReadout);
        return pRow;
    }

    internal static Slider PSFaderCreate(double pMinimum, double pMaximum, double pValue)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = Math.Max(pMaximum, pMinimum),
            SmallChange = 1,
            LargeChange = 1,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Value = Math.Clamp(pValue, pMinimum, Math.Max(pMaximum, pMinimum)),
            Width = PSFaderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        return pSlider;
    }

    internal static UIElement PSFaderRowBuild(Slider pSlider, FrameworkElement pTrailing)
    {
        pTrailing.Margin = new Thickness(12, 0, 0, 0);
        pTrailing.VerticalAlignment = VerticalAlignment.Center;
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = PSField.PSFieldControlHeight
        };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pTrailing);
        return pRow;
    }

    private static double PSFaderPositionResolve(double pKbps, double pMinimum, double pMaximum) =>
        Math.Log(pKbps / pMinimum) / Math.Log(pMaximum / pMinimum) * PSFaderBitrateTicks;

    private static double PSFaderBitrateResolve(double pPosition, double pMinimum, double pMaximum) =>
        pMinimum * Math.Pow(pMaximum / pMinimum, pPosition / PSFaderBitrateTicks);

    private static double? PSFaderBitrateParse(string pText)
    {
        pText = pText.Trim();
        if (pText.Length == 0)
        {
            return null;
        }

        char pUnit = pText[^1];
        string pNumber = char.IsDigit(pUnit) || pUnit == '.' ? pText : pText[..^1];
        if (!double.TryParse(pNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out double pValue) || pValue <= 0)
        {
            return null;
        }

        return pUnit switch
        {
            'k' or 'K' => pValue,
            'm' or 'M' => pValue * 1000,
            _ => pValue / 1000
        };
    }

    private static string PSFaderBitrateFormat(double pKbps)
    {
        double pRounded = Math.Round(pKbps);
        return pRounded >= 1000
            ? (pRounded / 1000).ToString("0.##", CultureInfo.InvariantCulture) + "M"
            : ((long)pRounded).ToString(CultureInfo.InvariantCulture) + "k";
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

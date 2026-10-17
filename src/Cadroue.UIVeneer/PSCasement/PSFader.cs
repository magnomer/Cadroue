using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal static partial class PSFader
{
    private const double PSFaderWidth = 220;

    internal static UIElement PSFaderQualityBuild(
        double pMinimum,
        double pMaximum,
        double pStep,
        string pValue,
        TextBox pReadout,
        bool pHigherBetter)
    {
        double pStart = PSFaderQualityParse(pValue, pMinimum, pMinimum, pMaximum, pStep);
        Slider pSlider = PSFaderStepBuild(
            pMinimum, pMaximum, pStep, PSFaderQualityResolve(pStart, pHigherBetter, pMinimum, pMaximum));
        PSFaderReadoutSet(pReadout, PSFaderValueFormat(pStart, pStep));
        var pSync = new StrongBox<bool>(false);
        PSFaderQualityAttach(pSlider, pReadout, pSync, pMinimum, pMaximum, pStep, pHigherBetter);
        return PSFaderRowBuild(pSlider, pReadout);
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
        pReadout.KeyDown += (_, pEvent) => PSFaderReturnHandle(
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

        PSFaderReadoutSet(
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
        PSFaderSyncSet(pSync, true);
        PSFaderPairSet(
            pSlider,
            pReadout,
            PSFaderQualityResolve(pQuality, pHigherBetter, pMinimum, pMaximum),
            PSFaderValueFormat(pQuality, pStep));
        PSFaderSyncSet(pSync, false);
    }

    internal static UIElement PSFaderLayoutBuild(
        Slider pSlider,
        IReadOnlyList<string> pLabels,
        int pIndex,
        TextBox pReadout,
        UIElement? pNotice = null)
    {
        int pLast = PSFaderLastResolve(pLabels);
        int pStart = PSFaderIndexClamp(pIndex, pLast);
        DoubleCollection pTicks = PSFaderTicksCreate(pLast);
        PSFaderLayoutApply(pSlider, pLast, pTicks, pStart);
        PSFaderEditApply(pReadout, true);
        PSFaderReadoutSet(pReadout, PSFaderLabelResolve(pLabels, pStart));
        PSFaderNoticeSet(pNotice, PSFaderNoticeResolve(pStart));
        PSFaderLayoutAttach(pSlider, pReadout, pLabels, pLast, pNotice);
        return PSFaderRowBuild(pSlider, pReadout);
    }

    private static int PSFaderLastResolve(IReadOnlyList<string> pLabels) => Math.Max(pLabels.Count - 1, 0);

    private static int PSFaderIndexClamp(double pPosition, int pLast) =>
        Math.Clamp((int)Math.Round(pPosition), 0, pLast);

    private static DoubleCollection PSFaderTicksCreate(int pLast)
    {
        var pTicks = new DoubleCollection();
        for (int pTick = 0; pTick <= pLast; pTick++)
        {
            pTicks.Add(pTick);
        }

        return pTicks;
    }

    private static void PSFaderLayoutApply(Slider pSlider, int pLast, DoubleCollection pTicks, int pStart)
    {
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
        QSlider.QSliderAttach(pSlider);
        pSlider.IsSnapToTickEnabled = true;
    }

    private static string PSFaderLabelResolve(IReadOnlyList<string> pLabels, int pAt) =>
        pLabels.Count > 0 ? pLabels[pAt] : string.Empty;

    private static void PSFaderLayoutAttach(
        Slider pSlider,
        TextBox pReadout,
        IReadOnlyList<string> pLabels,
        int pLast,
        UIElement? pNotice) =>
        pSlider.ValueChanged += (_, _) => PSFaderLayoutHandle(pSlider, pReadout, pLabels, pLast, pNotice);

    private static void PSFaderLayoutHandle(
        Slider pSlider,
        TextBox pReadout,
        IReadOnlyList<string> pLabels,
        int pLast,
        UIElement? pNotice)
    {
        int pAt = PSFaderIndexClamp(pSlider.Value, pLast);
        PSFaderReadoutSet(pReadout, PSFaderLabelResolve(pLabels, pAt));
        PSFaderNoticeSet(pNotice, PSFaderNoticeResolve(pAt));
    }

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
        double pTop = Math.Max(pMaximum, pMinimum);
        return PSFaderStepBuild(pMinimum, pTop, 1, Math.Clamp(pValue, pMinimum, pTop));
    }

    private static Slider PSFaderStepBuild(double pMinimum, double pMaximum, double pStep, double pValue)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            SmallChange = pStep,
            LargeChange = pStep,
            TickFrequency = pStep,
            IsSnapToTickEnabled = true,
            Value = pValue,
            Width = PSFaderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        QSlider.QSliderAttach(pSlider);
        return pSlider;
    }

    private static void PSFaderReadoutSet(TextBox pReadout, string pText) => pReadout.Text = pText;

    private static void PSFaderSliderSet(Slider pSlider, double pPosition) => pSlider.Value = pPosition;

    private static void PSFaderPairSet(Slider pSlider, TextBox pReadout, double pPosition, string pText)
    {
        pSlider.Value = pPosition;
        pReadout.Text = pText;
        pReadout.CaretIndex = pReadout.Text.Length;
    }

    private static void PSFaderSyncSet(StrongBox<bool> pSync, bool pActive) => pSync.Value = pActive;

    private static void PSFaderEditApply(TextBox pReadout, bool pReadOnly) => pReadout.IsReadOnly = pReadOnly;

    private static Visibility PSFaderNoticeResolve(double pAt) =>
        pAt <= 0 ? Visibility.Visible : Visibility.Collapsed;

    private static void PSFaderNoticeSet(UIElement? pNotice, Visibility pVisibility)
    {
        if (pNotice is not null)
        {
            pNotice.Visibility = pVisibility;
        }
    }

    private static void PSFaderReturnHandle(KeyEventArgs pEvent, Action pCommit)
    {
        if (pEvent.Key == Key.Return)
        {
            pCommit();
            pEvent.Handled = true;
        }
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

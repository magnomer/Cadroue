using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private TextBox PCropFieldBuild()
    {
        TextBox pRatioBox = PInspectorNumberBuild();
        pRatioBox.TextChanged += (_, _) => PCropRatioHandle();
        return pRatioBox;
    }

    private TextBox PInspectorInsetBuild(int pEdge)
    {
        TextBox pInsetBox = PInspectorNumberBuild();
        pInsetBox.TextChanged += (_, _) => PInspectorInsetChange(pEdge);
        return pInsetBox;
    }

    private static TextBox PInspectorNumberBuild()
    {
        var pNumberBox = new TextBox
        {
            Text = "0",
            Width = PInspectorInsetWidth,
            Height = PInspectorFieldHeight,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PTextbox.PTextboxApply(pNumberBox);
        pNumberBox.TextAlignment = TextAlignment.Center;
        pNumberBox.Padding = new Thickness(4, 0, 4, 0);
        pNumberBox.PreviewTextInput += (_, pNumberEvent) =>
            pNumberEvent.Handled = !pNumberEvent.Text.All(char.IsDigit);
        return pNumberBox;
    }

    private static TextBox PInspectorDecimalBuild()
    {
        var pDecimalBox = new TextBox
        {
            Width = PInspectorInsetWidth,
            Height = PInspectorFieldHeight,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PTextbox.PTextboxApply(pDecimalBox);
        pDecimalBox.TextAlignment = TextAlignment.Center;
        pDecimalBox.Padding = new Thickness(4, 0, 4, 0);
        pDecimalBox.PreviewTextInput += (_, pDecimalEvent) =>
            pDecimalEvent.Handled = !pDecimalEvent.Text.All(
                pChar => char.IsDigit(pChar) || pChar == '.' || pChar == '-');
        return pDecimalBox;
    }

    private static double PInspectorDecimalRead(TextBox pDecimalBox, double pFallback) =>
        double.TryParse(pDecimalBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double pValue)
            ? pValue
            : pFallback;

    private static Slider PInspectorSliderBuild(
        TextBox pValueBox,
        double pMin,
        double pMax,
        double pFallback,
        string pFormat,
        Func<double>? pResetRead,
        Action pChanged)
    {
        var pSlider = new Slider
        {
            Minimum = pMin,
            Maximum = pMax,
            Value = Math.Clamp(PInspectorDecimalRead(pValueBox, pFallback), pMin, pMax),
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        if (pResetRead is not null)
        {
            PSlider.PSliderResetApply(pSlider, pResetRead);
        }

        bool[] pSuppress = { false };
        pSlider.ValueChanged += (_, _) =>
        {
            if (pSuppress[0]) { return; }
            pSuppress[0] = true;
            pValueBox.Text = pSlider.Value.ToString(pFormat, CultureInfo.InvariantCulture);
            pSuppress[0] = false;
            pChanged();
        };
        pValueBox.TextChanged += (_, _) =>
        {
            if (pSuppress[0]) { return; }
            pSuppress[0] = true;
            pSlider.Value = Math.Clamp(PInspectorDecimalRead(pValueBox, pFallback), pMin, pMax);
            pSuppress[0] = false;
            pChanged();
        };
        return pSlider;
    }
}

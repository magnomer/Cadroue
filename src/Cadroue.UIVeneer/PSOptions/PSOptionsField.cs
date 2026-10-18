using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal static class PSOptionsField
{
    internal static CheckBox PSOptionsCheckBuild(string pLabel, bool pChecked)
    {
        var pCheck = new CheckBox
        {
            Content = pLabel,
            IsChecked = pChecked,
            VerticalAlignment = VerticalAlignment.Center
        };
        QInteractionClick.QInteractionClickAttach(pCheck);
        QCheckbox.QCheckboxAttach(pCheck);
        return pCheck;
    }

    internal static Slider PSOptionsSliderBuild(double pValue, double pMinimum, double pMaximum)
    {
        double pClamped = PSOptionsSliderClamp(pValue, pMinimum, pMaximum);
        return PSOptionsSliderCreate(pClamped, pMinimum, pMaximum);
    }

    private static double PSOptionsSliderClamp(double pValue, double pMinimum, double pMaximum) =>
        Math.Clamp(pValue, pMinimum, pMaximum);

    private static Slider PSOptionsSliderCreate(double pValue, double pMinimum, double pMaximum)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            Value = pValue,
            Width = 260
        };
        QSlider.QSliderAttach(pSlider);
        return pSlider;
    }

    internal static UIElement PSOptionsFieldBuild(string pLabel, Slider pSlider, string pUnit)
    {
        TextBlock pValueText = PSOptionsValueBuild();
        PSOptionsValueUpdate(pValueText, pSlider, pUnit);
        PSOptionsValueAttach(pValueText, pSlider, pUnit);
        return PSOptionsRowBuild(pLabel, pSlider, pValueText);
    }

    private static TextBlock PSOptionsValueBuild() => new()
    {
        Foreground = PSField.PSFieldText,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(12, 0, 0, 0),
        MinWidth = 48
    };

    private static void PSOptionsValueUpdate(TextBlock pValueText, Slider pSlider, string pUnit) =>
        PSOptionsValueShow(pValueText, PSOptionsValueFormat(pSlider.Value, pUnit));

    private static void PSOptionsValueAttach(TextBlock pValueText, Slider pSlider, string pUnit) =>
        pSlider.ValueChanged += (_, _) => PSOptionsValueUpdate(pValueText, pSlider, pUnit);

    private static void PSOptionsValueShow(TextBlock pValueText, string pText) => pValueText.Text = pText;

    private static UIElement PSOptionsRowBuild(string pLabel, Slider pSlider, TextBlock pValueText)
    {
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pValueText);
        return PSField.PSFieldBuild(pLabel, pRow);
    }

    private static string PSOptionsValueFormat(double pValue, string pUnit) => PSOptionsNumberFormat(pValue) + pUnit;

    private static string PSOptionsNumberFormat(double pValue) => $"{Math.Round(pValue):0}";

    internal static int PSOptionsNumberNormalize(double pValue) => (int)Math.Round(pValue);

    internal static bool PSOptionsCheckRead(CheckBox pCheck) => pCheck.IsChecked == true;

    internal static bool PSOptionsModeCheck(Border pMode, string pKey) =>
        string.Equals(PSMode.PSModeTextRead(pMode), pKey, StringComparison.Ordinal);

    internal static StackPanel PSSystemRowBuild(Button pButton)
    {
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pRow.Children.Add(pButton);
        return pRow;
    }
}

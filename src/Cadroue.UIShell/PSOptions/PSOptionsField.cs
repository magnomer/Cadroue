using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PHouse;

using static Cadroue.UIShell.PSCasement.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions
{
    private static CheckBox PSOptionsCheckBuild(string pLabel, bool pChecked)
    {
        var pCheck = new CheckBox
        {
            Content = pLabel,
            IsChecked = pChecked,
            VerticalAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pCheck);
        return pCheck;
    }

    private static Slider PSOptionsSliderBuild(double pValue, double pMinimum, double pMaximum)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            Value = Math.Clamp(pValue, pMinimum, pMaximum),
            Width = 260
        };
        PSlider.PSliderApply(pSlider);
        return pSlider;
    }

    private static UIElement PSOptionsFieldBuild(string pLabel, Slider pSlider, string pUnit)
    {
        var pValueText = new TextBlock
        {
            Foreground = PSFieldText,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            MinWidth = 48,
            Text = PSOptionsNumberFormat(pSlider.Value) + pUnit
        };
        pSlider.ValueChanged += (_, _) => pValueText.Text = PSOptionsNumberFormat(pSlider.Value) + pUnit;

        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pValueText);
        return PSFieldBuild(pLabel, pRow);
    }

    private static string PSOptionsNumberFormat(double pValue) => $"{Math.Round(pValue):0}";
}

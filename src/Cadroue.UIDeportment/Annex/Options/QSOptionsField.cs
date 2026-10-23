using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSOptionsField
{
    private readonly List<(Slider, RoutedPropertyChangedEventHandler<double>)> qsOptionsField = [];

    public void QSOptionsFieldAttach(Slider slider, TextBlock readout, string unit)
    {
        RoutedPropertyChangedEventHandler<double> update = (_, _) =>
            readout.Text = QSOptionsFieldFormat(slider.Value, unit);
        readout.Text = QSOptionsFieldFormat(slider.Value, unit);
        slider.ValueChanged += update;
        qsOptionsField.Add((slider, update));
    }

    public void QSOptionsFieldDetach()
    {
        foreach ((Slider slider, RoutedPropertyChangedEventHandler<double> update) in qsOptionsField)
        {
            slider.ValueChanged -= update;
        }
        qsOptionsField.Clear();
    }

    public static void QSOptionsFieldPrepare(CheckBox box, string label, bool value)
    {
        box.Content = label;
        box.IsChecked = value;
        QInteractionClick.QInteractionClickAttach(box);
        QCheckbox.QCheckboxAttach(box);
    }

    public static void QSOptionsFieldPrepare(Slider slider, double value)
    {
        slider.Value = Math.Clamp(value, slider.Minimum, slider.Maximum);
        QSlider.QSliderAttach(slider);
    }

    public static bool QSOptionsFieldRead(CheckBox box) => box.IsChecked == true;

    private static string QSOptionsFieldFormat(double value, string unit) => $"{Math.Round(value):0}" + unit;
}

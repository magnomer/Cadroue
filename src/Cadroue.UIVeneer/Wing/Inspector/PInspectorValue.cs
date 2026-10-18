using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PInspectorValue
{
    internal static readonly IReadOnlyDictionary<Key, bool> PInspectorValueHandled = new Dictionary<Key, bool>
    {
        [Key.Enter] = true,
    };

    internal static Slider PInspectorSliderBuild(double pLeast, double pMost, double pValue, Func<double> pReset)
    {
        var pSlider = new Slider
        {
            Minimum = pLeast,
            Maximum = pMost,
            Value = pValue,
            VerticalAlignment = VerticalAlignment.Center
        };
        QSlider.QSliderAttach(pSlider);
        QSlider.QSliderResetAttach(pSlider, pReset);
        return pSlider;
    }

    internal static void PInspectorValueUpdate(Slider pSlider, TextBox pValue, double pNumber, string pFormat)
    {
        double pClamped = PInspectorValueClamp(pSlider, pNumber);
        PInspectorSliderSet(pSlider, pClamped);
        PInspectorNumber.PInspectorTextSet(pValue, pNumber, pFormat);
    }

    private static double PInspectorValueClamp(Slider pSlider, double pNumber) =>
        Math.Clamp(pNumber, pSlider.Minimum, pSlider.Maximum);

    private static void PInspectorSliderSet(Slider pSlider, double pNumber) => pSlider.Value = pNumber;

    internal static void PInspectorValueAttach(
        Slider pSlider,
        TextBox pValue,
        double? pMinimum,
        double? pMaximum,
        Func<double> pRead,
        Action<double> pSet)
    {
        var pValueKeys = new Dictionary<Key, Action>
        {
            [Key.Enter] = () => PInspectorValueCommit(pValue, pMinimum, pMaximum, pRead, pSet),
        };
        pSlider.ValueChanged += (_, _) => PInspectorSlideCommit(pSlider, pRead, pSet);
        pValue.LostFocus += (_, _) => PInspectorValueCommit(pValue, pMinimum, pMaximum, pRead, pSet);
        pValue.KeyDown += (_, pKeyEvent) =>
        {
            pValueKeys.GetValueOrDefault(pKeyEvent.Key)?.Invoke();
            pKeyEvent.Handled = PInspectorValueHandled.GetValueOrDefault(pKeyEvent.Key);
        };
    }

    private static void PInspectorSlideCommit(Slider pSlider, Func<double> pRead, Action<double> pSet) =>
        QInspectorField.QInspectorSlideCommit(pSlider.Value, pRead(), pSlider.Minimum, pSlider.Maximum, pSet);

    private static void PInspectorValueCommit(
        TextBox pValue, double? pMinimum, double? pMaximum, Func<double> pRead, Action<double> pSet)
    {
        pSet(QInspectorField.QInspectorValueCommit(pValue.Text, pRead(), pMinimum, pMaximum));
        PInspectorNumber.PInspectorCaretPlace(pValue);
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private static readonly IReadOnlyDictionary<Key, bool> pInspectorValueHandled = new Dictionary<Key, bool>
    {
        [Key.Enter] = true,
    };

    private static void PInspectorSectionApply(
        CheckBox pBox,
        CheckBox pPersistent,
        StackPanel pStack,
        StackPanel pBody,
        QInspectorTip pTip)
    {
        PInspectorEnabledApply(pBox, pTip.QInspectorTipEnabled);
        PInspectorEnabledApply(pPersistent, pTip.QInspectorTipEnabled);
        PInspectorSectionUpdate(pStack, pTip.QInspectorTipEnabled);
        PInspectorTipApply(pBox, pPersistent, pBody, pTip);
    }

    private static void PInspectorTipApply(CheckBox pBox, CheckBox pPersistent, StackPanel pBody, QInspectorTip pTip)
    {
        pBody.ToolTip = pTip.QInspectorTipNotice;
        pBox.ToolTip = pTip.QInspectorTipBox;
        pPersistent.ToolTip = pTip.QInspectorTipPersistent;
        ToolTipService.SetShowOnDisabled(pBody, true);
        ToolTipService.SetShowOnDisabled(pBox, true);
        ToolTipService.SetShowOnDisabled(pPersistent, true);
    }

    private static void PInspectorValueUpdate(Slider pSlider, TextBox pValue, double pNumber, string pFormat)
    {
        double pClamped = PInspectorValueClamp(pSlider, pNumber);
        PInspectorSliderSet(pSlider, pClamped);
        PInspectorTextSet(pValue, pNumber, pFormat);
    }

    private static double PInspectorValueClamp(Slider pSlider, double pNumber) =>
        Math.Clamp(pNumber, pSlider.Minimum, pSlider.Maximum);

    private static void PInspectorSliderSet(Slider pSlider, double pNumber) => pSlider.Value = pNumber;

    private static void PInspectorSwitchUpdate(CheckBox pBox, bool pChecked) =>
        pBox.IsChecked = PLook.PLookChecked[pChecked];

    private static Slider PToneSliderBuild(double pMinimum, double pMaximum, double pValue)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            Value = pValue,
            VerticalAlignment = VerticalAlignment.Center
        };
        QSlider.QSliderAttach(pSlider);
        QSlider.QSliderResetAttach(pSlider, () => pValue);
        return pSlider;
    }

    private static StackPanel PToneBodyBuild(CheckBox pApply, StackPanel pStack)
    {
        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);
        return pBody;
    }

    private static void PInspectorSwitchAttach(CheckBox pBox, Action<bool> pSet)
    {
        pBox.Checked += (_, _) => pSet(true);
        pBox.Unchecked += (_, _) => pSet(false);
    }

    private static void PInspectorValueAttach(
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
            pKeyEvent.Handled = pInspectorValueHandled.GetValueOrDefault(pKeyEvent.Key);
        };
    }

    private static void PInspectorSlideCommit(Slider pSlider, Func<double> pRead, Action<double> pSet) =>
        QInspector.QInspectorSlideCommit(pSlider.Value, pRead(), pSlider.Minimum, pSlider.Maximum, pSet);

    private static void PInspectorValueCommit(
        TextBox pValue, double? pMinimum, double? pMaximum, Func<double> pRead, Action<double> pSet)
    {
        pSet(QInspector.QInspectorValueCommit(pValue.Text, pRead(), pMinimum, pMaximum));
        PInspectorCaretPlace(pValue);
    }

    private static void PInspectorCaretPlace(TextBox pValue) => pValue.CaretIndex = pValue.Text.Length;
}

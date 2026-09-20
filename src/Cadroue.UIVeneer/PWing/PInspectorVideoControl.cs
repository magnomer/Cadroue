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
        LInspectorTip pTip)
    {
        pBox.IsEnabled = pTip.LInspectorTipEnabled;
        pPersistent.IsEnabled = pTip.LInspectorTipEnabled;
        PInspectorSectionUpdate(pStack, pTip.LInspectorTipEnabled);
        pBody.ToolTip = pTip.LInspectorTipNotice;
        pBox.ToolTip = pTip.LInspectorTipBox;
        pPersistent.ToolTip = pTip.LInspectorTipPersistent;
        ToolTipService.SetShowOnDisabled(pBody, true);
        ToolTipService.SetShowOnDisabled(pBox, true);
        ToolTipService.SetShowOnDisabled(pPersistent, true);
    }

    private static void PInspectorValueUpdate(Slider pSlider, TextBox pValue, double pNumber, string pFormat)
    {
        pSlider.Value = Math.Clamp(pNumber, pSlider.Minimum, pSlider.Maximum);
        PInspectorTextSet(pValue, pNumber, pFormat);
    }

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
        PSlider.PSliderApply(pSlider);
        PSlider.PSliderResetApply(pSlider, () => pValue);
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
        LInspector.LInspectorSlideCommit(pSlider.Value, pRead(), pSlider.Minimum, pSlider.Maximum, pSet);

    private static void PInspectorValueCommit(
        TextBox pValue, double? pMinimum, double? pMaximum, Func<double> pRead, Action<double> pSet)
    {
        pSet(LInspector.LInspectorValueCommit(pValue.Text, pRead(), pMinimum, pMaximum));
        pValue.CaretIndex = pValue.Text.Length;
    }
}

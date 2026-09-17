using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private static void PInspectorSectionApply(
        CheckBox pBox,
        CheckBox pPersistent,
        StackPanel pStack,
        StackPanel pBody,
        bool pActive,
        bool pCapable,
        bool pPreviewAvailable,
        string pDisabledKey,
        string pPreviewKey,
        string pApplyKey,
        string pPersistKey)
    {
        pBox.IsEnabled = pCapable;
        pPersistent.IsEnabled = pCapable;
        PInspectorSectionUpdate(pStack, pCapable && pActive);
        string? pNotice = !pCapable
            ? LLocalization.LLocalizationTextRead(pDisabledKey)
            : !pPreviewAvailable && pPreviewKey.Length > 0
                ? LLocalization.LLocalizationTextRead(pPreviewKey)
                : null;
        pBody.ToolTip = pNotice;
        pBox.ToolTip = pNotice ?? LLocalization.LLocalizationTextRead(pApplyKey);
        pPersistent.ToolTip = pNotice ?? LLocalization.LLocalizationTextRead(pPersistKey);
        ToolTipService.SetShowOnDisabled(pBody, true);
        ToolTipService.SetShowOnDisabled(pBox, true);
        ToolTipService.SetShowOnDisabled(pPersistent, true);
    }

    private static void PInspectorValueUpdate(Slider pSlider, TextBox pValue, double pNumber, string pFormat)
    {
        pSlider.Value = Math.Clamp(pNumber, pSlider.Minimum, pSlider.Maximum);
        PInspectorTextSet(pValue, pNumber, pFormat);
    }

    private void PInspectorSwitchUpdate(CheckBox pBox, bool pChecked, bool pPlan)
    {
        if ((pBox.IsChecked == true) == pChecked)
        {
            return;
        }

        pBox.IsChecked = pChecked;
        if (pPlan)
        {
            PInspectorPlanChange?.Invoke();
        }
    }

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
        pSlider.ValueChanged += (_, _) =>
        {
            if (pSlider.Value != Math.Clamp(pRead(), pSlider.Minimum, pSlider.Maximum))
            {
                pSet(pSlider.Value);
            }
        };
        pValue.TextChanged += (_, _) =>
        {
            double pParsed = PInspectorDecimalRead(pValue, pRead());
            if (pMinimum is double pMin && pMaximum is double pMax)
            {
                pParsed = Math.Clamp(pParsed, pMin, pMax);
            }

            pSet(pParsed);
        };
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private static void PInspectorSectionApply(
        CheckBox pBox,
        CheckBox pPersistent,
        StackPanel pStack,
        StackPanel pBody,
        bool pCapable,
        bool pPreviewAvailable,
        string pDisabledKey,
        string pPreviewKey,
        string pApplyKey,
        string pPersistKey)
    {
        pBox.IsEnabled = pCapable;
        pPersistent.IsEnabled = pCapable;
        pStack.IsEnabled = pCapable && pBox.IsChecked == true;
        pStack.Opacity = pCapable && pBox.IsChecked == true ? 1 : 0.4;
        string? pNotice = !pCapable
            ? LLocalization.LLocalizationTextRead(pDisabledKey)
            : !pPreviewAvailable
                ? LLocalization.LLocalizationTextRead(pPreviewKey)
                : null;
        pBody.ToolTip = pNotice;
        pBox.ToolTip = pNotice ?? LLocalization.LLocalizationTextRead(pApplyKey);
        pPersistent.ToolTip = pNotice ?? LLocalization.LLocalizationTextRead(pPersistKey);
        ToolTipService.SetShowOnDisabled(pBody, true);
        ToolTipService.SetShowOnDisabled(pBox, true);
        ToolTipService.SetShowOnDisabled(pPersistent, true);
    }

    private static void PInspectorValueSet(Slider pSlider, TextBox pValue, double pNumber)
    {
        pSlider.Value = pNumber;
        pValue.Text = pNumber.ToString("0.#", CultureInfo.InvariantCulture);
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

    private void PInspectorVideoAttach(
        CheckBox pApply,
        StackPanel pStack,
        Slider pSlider,
        TextBox pValue,
        double? pMinimum,
        double? pMaximum,
        string pFormat)
    {
        pApply.Checked += (_, _) => PToneApplyUpdate(pApply, pStack);
        pApply.Unchecked += (_, _) => PToneApplyUpdate(pApply, pStack);
        PInspectorValueAttach(pSlider, pValue, pMinimum, pMaximum, pFormat);
    }

    private void PInspectorValueAttach(
        Slider pSlider,
        TextBox pValue,
        double? pMinimum,
        double? pMaximum,
        string pFormat)
    {
        pSlider.ValueChanged += (_, _) =>
        {
            if (pInspectorVideoSuppress)
            {
                return;
            }

            pInspectorVideoSuppress = true;
            pValue.Text = pSlider.Value.ToString(pFormat, CultureInfo.InvariantCulture);
            pInspectorVideoSuppress = false;
            PInspectorVideoChange?.Invoke();
        };
        pValue.TextChanged += (_, _) =>
        {
            if (pInspectorVideoSuppress)
            {
                return;
            }

            pInspectorVideoSuppress = true;
            double pParsed = PInspectorDecimalRead(pValue, pSlider.Value);
            if (pMinimum is double pMin && pMaximum is double pMax)
            {
                pParsed = Math.Clamp(pParsed, pMin, pMax);
            }

            pSlider.Value = Math.Clamp(pParsed, pSlider.Minimum, pSlider.Maximum);
            pInspectorVideoSuppress = false;
            PInspectorVideoChange?.Invoke();
        };
    }

    private void PToneApplyUpdate(CheckBox pApply, StackPanel pStack)
    {
        bool pActive = pApply.IsChecked == true;
        pStack.IsEnabled = pActive;
        pStack.Opacity = pActive ? 1 : 0.4;
        if (!pActive && ReferenceEquals(pApply, pWhitebalanceBox))
        {
            PWhitebalanceToolReset();
        }

        if (!pInspectorVideoSuppress)
        {
            PInspectorVideoChange?.Invoke();
        }
    }
}

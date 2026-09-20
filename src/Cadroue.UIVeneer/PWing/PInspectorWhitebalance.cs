using System.Windows;
using System.Windows.Controls;

using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pWhitebalanceBox = null!;
    private CheckBox pWhitebalancePersistent = null!;
    private ComboBox pWhitebalanceMethod = null!;
    private Slider pWhitebalanceSaturationSlider = null!;
    private TextBox pWhitebalanceSaturationValue = null!;
    private StackPanel pWhitebalanceStack = null!;
    private StackPanel pWhitebalanceBody = null!;
    private UIElement pWhitebalanceMethodField = null!;
    private UIElement pWhitebalanceSaturationField = null!;

    private StackPanel PWhitebalanceBuild()
    {
        pWhitebalanceBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyWhitebalance"));
        pWhitebalancePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistWhitebalance"));
        PInspectorSwitchAttach(pWhitebalanceBox, LWhitebalance.LWhitebalanceActiveSet);
        PInspectorSwitchAttach(pWhitebalancePersistent, LWhitebalance.LWhitebalancePersistentSet);
        pWhitebalanceMethod = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pWhitebalanceMethod);
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Average", "Inspector.Video.WhitebalanceMethodAverage"));
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Minmax", "Inspector.Video.WhitebalanceMethodMinmax"));
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Median", "Inspector.Video.WhitebalanceMethodMedian"));
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Custom", "Inspector.Video.WhitebalanceMethodCustom"));
        pWhitebalanceMethod.SelectedIndex = 2;
        pWhitebalanceMethod.SelectionChanged += (_, _) =>
            LWhitebalance.LWhitebalanceMethodSelect(pWhitebalanceMethod.SelectedIndex);

        pWhitebalanceSaturationSlider = PToneSliderBuild(0, 300, 100);
        pWhitebalanceSaturationValue = PInspectorDecimalBuild();
        pWhitebalanceSaturationValue.Text = "100";
        PInspectorValueAttach(
            pWhitebalanceSaturationSlider,
            pWhitebalanceSaturationValue,
            0,
            300,
            () => LWhitebalance.LWhitebalanceValue.LWorkWhitebalanceSaturation,
            LWhitebalance.LWhitebalanceSaturationSet);
        pWhitebalanceStack = new StackPanel();
        pWhitebalanceMethodField = PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceMethod"),
            pWhitebalanceMethod);
        pWhitebalanceSaturationField = PInspectorSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceSaturation"),
            pWhitebalanceSaturationSlider,
            "%",
            pWhitebalanceSaturationValue);
        pWhitebalanceStack.Children.Add(PToneNeutralBuild());
        pWhitebalanceStack.Children.Add(PWhitebalanceWheelBuild());
        pWhitebalanceStack.Children.Add(pWhitebalanceMethodField);
        pWhitebalanceStack.Children.Add(pWhitebalanceSaturationField);
        pWhitebalanceStack.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceWarning"),
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x64, 0x70, 0x82)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 8)
        });
        var pWhitebalanceReset = new Button
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceReset"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceResetTooltip"),
            Height = 28,
            MinWidth = 64,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Style = PButton.PButtonPanelCreate(),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pWhitebalanceReset.Click += (_, _) => LWhitebalance.LWhitebalanceReset();
        pWhitebalanceStack.Children.Add(pWhitebalanceReset);
        pWhitebalanceBody = PToneBodyBuild(pWhitebalanceBox, pWhitebalanceStack);
        return pWhitebalanceBody;
    }

    private void PWhitebalanceUpdate()
    {
        PInspectorSwitchUpdate(pWhitebalanceBox, LWhitebalance.LWhitebalanceStep.LWorkStepActive);
        PInspectorSwitchUpdate(pWhitebalancePersistent, LWhitebalance.LWhitebalancePersistent);
        pWhitebalanceMethod.SelectedIndex = LWhitebalance.LWhitebalanceMethodIndex;
        PInspectorValueUpdate(
            pWhitebalanceSaturationSlider,
            pWhitebalanceSaturationValue,
            LWhitebalance.LWhitebalanceValue.LWorkWhitebalanceSaturation,
            "0.#");
        LInspectorTip pTip = LInspector.LInspectorTipResolve(
            LWhitebalance.LWhitebalanceStep.LWorkStepActive,
            LWhitebalance.LWhitebalanceCapable,
            LWhitebalance.LWhitebalancePreview,
            "Inspector.Video.WhitebalanceRequiresEq",
            "Inspector.Video.WhitebalancePreviewMpv",
            "Inspector.Video.ApplyWhitebalance",
            "Inspector.Video.PersistWhitebalance");
        PInspectorSectionApply(
            pWhitebalanceBox, pWhitebalancePersistent, pWhitebalanceStack, pWhitebalanceBody, pTip);
        pInspectorNeutralTool.IsEnabled = LWhitebalance.LWhitebalanceCapable;
        pInspectorNeutralTool.ToolTip = LInspector.LInspectorNoticeRead(
            pTip, "Inspector.Video.WhitebalancePickTooltip");
        pInspectorWhiteTool.IsEnabled = LWhitebalance.LWhitebalanceCapable;
        pInspectorWhiteTool.ToolTip = LInspector.LInspectorNoticeRead(
            pTip, "Inspector.Video.WhitebalancePickWhiteTooltip");
        ToolTipService.SetShowOnDisabled(pInspectorNeutralTool, true);
        ToolTipService.SetShowOnDisabled(pInspectorWhiteTool, true);
        PWhitebalanceToolUpdate();
        PWhitebalanceReadoutUpdate();
        PWhitebalanceWheelPlace();
    }
}

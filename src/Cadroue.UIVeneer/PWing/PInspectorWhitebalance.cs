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
        StackPanel pBody = PWhitebalancePanelBuild();
        PWhitebalanceAttach();
        return pBody;
    }

    private StackPanel PWhitebalancePanelBuild()
    {
        pWhitebalanceBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyWhitebalance"));
        pWhitebalancePersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistWhitebalance"));
        pWhitebalanceMethod = PInspectorComboBuild();
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Average", "Inspector.Video.WhitebalanceMethodAverage"));
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Minmax", "Inspector.Video.WhitebalanceMethodMinmax"));
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Median", "Inspector.Video.WhitebalanceMethodMedian"));
        pWhitebalanceMethod.Items.Add(new LLocalizationChoice(
            "Custom", "Inspector.Video.WhitebalanceMethodCustom"));
        pWhitebalanceMethod.SelectedIndex = 2;

        pWhitebalanceSaturationSlider = PToneSliderBuild(0, 300, 100);
        pWhitebalanceSaturationValue = PInspectorDecimalBuild();
        pWhitebalanceSaturationValue.Text = "100";
        pWhitebalanceStack = new StackPanel();
        pWhitebalanceMethodField = PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceMethod"),
            pWhitebalanceMethod);
        pWhitebalanceSaturationField = PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceSaturation"),
            pWhitebalanceSaturationSlider,
            "%",
            pWhitebalanceSaturationValue);
        pWhitebalanceStack.Children.Add(PToneNeutralBuild());
        pWhitebalanceStack.Children.Add(PWhitebalanceWheelBuild());
        pWhitebalanceStack.Children.Add(pWhitebalanceMethodField);
        pWhitebalanceStack.Children.Add(pWhitebalanceSaturationField);
        pWhitebalanceStack.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceWarning"),
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x64, 0x70, 0x82)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 8)
        });
        pWhitebalanceStack.Children.Add(PWhitebalanceResetBuild());
        pWhitebalanceBody = PToneBodyBuild(pWhitebalanceBox, pWhitebalanceStack);
        return pWhitebalanceBody;
    }

    private void PWhitebalanceAttach()
    {
        PInspectorSwitchAttach(pWhitebalanceBox, QWhitebalance.QWhitebalanceActiveSet);
        PInspectorSwitchAttach(pWhitebalancePersistent, QWhitebalance.QWhitebalancePersistentSet);
        pWhitebalanceMethod.SelectionChanged += (_, _) =>
            QWhitebalance.QWhitebalanceMethodSelect(pWhitebalanceMethod.SelectedIndex);
        PInspectorValueAttach(
            pWhitebalanceSaturationSlider,
            pWhitebalanceSaturationValue,
            0,
            300,
            () => QWhitebalance.QWhitebalanceValue.LWorkWhitebalanceSaturation,
            QWhitebalance.QWhitebalanceSaturationSet);
    }

    private Button PWhitebalanceResetBuild()
    {
        Button pWhitebalanceReset = PWhitebalanceCaptionBuild();
        PInspectorClickAttach(pWhitebalanceReset, () => QWhitebalance.QWhitebalanceReset());
        QButton.QButtonAttach(pWhitebalanceReset, QButtonLook.QButtonLookPanel);
        return pWhitebalanceReset;
    }

    private static Button PWhitebalanceCaptionBuild() => new()
    {
        Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceReset"),
        ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceResetTooltip"),
        Height = 28,
        MinWidth = 64,
        Padding = new Thickness(8, 0, 8, 0),
        FontSize = 11,
        FontFamily = pInspectorFontFamily,
        HorizontalAlignment = HorizontalAlignment.Right
    };

    private void PWhitebalanceUpdate()
    {
        PWhitebalanceStateApply();
        QInspectorTip pTip = QInspector.QInspectorTipResolve(
            QWhitebalance.QWhitebalanceStep.LWorkStepActive,
            QWhitebalance.QWhitebalanceCapable,
            QWhitebalance.QWhitebalancePreview,
            "Inspector.Video.WhitebalanceRequiresEq",
            "Inspector.Video.WhitebalancePreviewMpv",
            "Inspector.Video.ApplyWhitebalance",
            "Inspector.Video.PersistWhitebalance");
        PInspectorSectionApply(
            pWhitebalanceBox, pWhitebalancePersistent, pWhitebalanceStack, pWhitebalanceBody, pTip);
        PWhitebalancePickerApply(pTip);
        PWhitebalanceToolUpdate();
        PWhitebalanceReadoutUpdate();
        PWhitebalanceWheelPlace();
    }

    private void PWhitebalanceStateApply()
    {
        PInspectorSwitchUpdate(pWhitebalanceBox, QWhitebalance.QWhitebalanceStep.LWorkStepActive);
        PInspectorSwitchUpdate(pWhitebalancePersistent, QWhitebalance.QWhitebalancePersistent);
        pWhitebalanceMethod.SelectedIndex = QWhitebalance.QWhitebalanceMethodIndex;
        PInspectorValueUpdate(
            pWhitebalanceSaturationSlider,
            pWhitebalanceSaturationValue,
            QWhitebalance.QWhitebalanceValue.LWorkWhitebalanceSaturation,
            "0.#");
    }

    private void PWhitebalancePickerApply(QInspectorTip pTip)
    {
        pInspectorNeutralTool.IsEnabled = QWhitebalance.QWhitebalanceCapable;
        pInspectorNeutralTool.ToolTip = QInspector.QInspectorNoticeRead(
            pTip, "Inspector.Video.WhitebalancePickTooltip");
        pInspectorWhiteTool.IsEnabled = QWhitebalance.QWhitebalanceCapable;
        pInspectorWhiteTool.ToolTip = QInspector.QInspectorNoticeRead(
            pTip, "Inspector.Video.WhitebalancePickWhiteTooltip");
        ToolTipService.SetShowOnDisabled(pInspectorNeutralTool, true);
        ToolTipService.SetShowOnDisabled(pInspectorWhiteTool, true);
    }
}

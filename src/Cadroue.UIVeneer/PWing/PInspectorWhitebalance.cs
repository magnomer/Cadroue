using System.Windows;
using System.Windows.Controls;

using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pWhitebalanceBox = null!;
    private CheckBox pWhitebalancePersistent = null!;
    private ComboBox pWhitebalanceMethod = null!;
    private StackPanel pWhitebalanceStack = null!;
    private StackPanel pWhitebalanceBody = null!;

    // The panel's parts first; the driver then fills the method rows and the saturation field and takes the inputs,
    // and the wheel is drawn at its slider's brightness.
    private StackPanel PWhitebalanceBuild()
    {
        StackPanel pBody = PWhitebalancePanelBuild();
        QWhitebalance = new QWhitebalance(
            QInspector,
            pWhitebalanceBox,
            pWhitebalancePersistent,
            pWhitebalanceMethod,
            pWhitebalanceStack,
            pWhitebalanceBody,
            [pInspectorNeutralTool, pInspectorWhiteTool],
            pWhitebalanceWheelDot,
            PWhitebalanceWheelSize);
        PWhitebalanceBrightnessHandle();
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
        pWhitebalanceStack = new StackPanel();
        pWhitebalanceStack.Children.Add(PToneNeutralBuild());
        pWhitebalanceStack.Children.Add(PWhitebalanceWheelBuild());
        pWhitebalanceStack.Children.Add(PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceMethod"),
            pWhitebalanceMethod));
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

    // The driver paints the section, the fields and the wheel dot; the picker row is painted here.
    private void PWhitebalanceUpdate()
    {
        PWhitebalancePickerApply(QWhitebalance.QWhitebalanceTip);
        PWhitebalanceToolUpdate();
        PWhitebalanceReadoutUpdate();
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

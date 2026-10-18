using System.Windows;
using System.Windows.Controls;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PInspectorWhitebalance
{
    private readonly PInspectorWhitebalanceNeutral pWhitebalanceNeutral;
    private readonly PInspectorWhitebalanceWheel pWhitebalanceWheel;
    private CheckBox pWhitebalanceBox = null!;
    private ComboBox pWhitebalanceMethod = null!;
    private StackPanel pWhitebalanceStack = null!;

    // The panel's parts first; the driver then fills the method rows and the saturation field and takes the inputs,
    // and the wheel is drawn at its slider's brightness.
    internal PInspectorWhitebalance(QInspector lInspector)
    {
        pWhitebalanceNeutral = new PInspectorWhitebalanceNeutral();
        pWhitebalanceWheel = new PInspectorWhitebalanceWheel();
        PWhitebalancePanelBuild();
        QWhitebalance = new QWhitebalance(
            lInspector,
            pWhitebalanceBox,
            PWhitebalancePersistent,
            pWhitebalanceMethod,
            pWhitebalanceStack,
            PWhitebalanceBody,
            [pWhitebalanceNeutral.PInspectorNeutralTool, pWhitebalanceNeutral.PInspectorWhiteTool],
            pWhitebalanceWheel.PWhitebalanceWheelDot,
            PInspectorWhitebalanceWheel.PWhitebalanceWheelSize);
        pWhitebalanceWheel.PWhitebalanceWheelAttach(QWhitebalance.QWhitebalanceWheel);
    }

    internal QWhitebalance QWhitebalance { get; }

    internal CheckBox PWhitebalancePersistent { get; private set; } = null!;

    internal StackPanel PWhitebalanceBody { get; private set; } = null!;

    private void PWhitebalancePanelBuild()
    {
        pWhitebalanceBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyWhitebalance"));
        PWhitebalancePersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistWhitebalance"));
        pWhitebalanceMethod = PInspectorChoice.PInspectorComboBuild();
        pWhitebalanceStack = new StackPanel();
        pWhitebalanceStack.Children.Add(pWhitebalanceNeutral.PInspectorNeutralColumn);
        pWhitebalanceStack.Children.Add(pWhitebalanceWheel.PWhitebalancePalette);
        pWhitebalanceStack.Children.Add(PInspectorField.PInspectorFieldBuild(
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
        PWhitebalanceBody = PInspectorField.PToneBodyBuild(pWhitebalanceBox, pWhitebalanceStack);
    }

    private Button PWhitebalanceResetBuild()
    {
        Button pWhitebalanceReset = PWhitebalanceCaptionBuild();
        PInspectorButton.PInspectorClickAttach(pWhitebalanceReset, () => QWhitebalance.QWhitebalanceReset());
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
        FontFamily = PInspectorLook.PInspectorFontFamily,
        HorizontalAlignment = HorizontalAlignment.Right
    };

    internal void PWhitebalanceChangeAttach() => QWhitebalance.QWhitebalanceChanged += PWhitebalanceUpdate;

    // The driver paints the section, the fields and the wheel dot; the picker row is painted here.
    internal void PWhitebalanceUpdate()
    {
        pWhitebalanceNeutral.PWhitebalancePickerApply(QWhitebalance, QWhitebalance.QWhitebalanceTip);
        pWhitebalanceNeutral.PWhitebalanceToolUpdate(QWhitebalance);
        pWhitebalanceNeutral.PWhitebalanceReadoutUpdate(QWhitebalance);
    }
}

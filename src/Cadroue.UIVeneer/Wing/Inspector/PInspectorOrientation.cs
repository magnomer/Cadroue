using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorOrientation
{
    private readonly CheckBox pInspectorFlipHorizontal;
    private readonly CheckBox pInspectorFlipVertical;
    private readonly ComboBox pInspectorRotateCombo;

    internal PInspectorOrientation(QInspectorOrientation lOrientation)
    {
        QInspectorOrientation = lOrientation;
        pInspectorFlipHorizontal = PCropCheckBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Inspector.Crop.Horizontal"));
        pInspectorFlipVertical = PCropCheckBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Vertical"));
        pInspectorRotateCombo = PInspectorRotateBuild();
        PInspectorFlipField = PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Flip"), PCropFlipBuild());
        PInspectorRotateField = PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Rotate"), pInspectorRotateCombo);
        PInspectorSwitch.PInspectorSwitchAttach(
            pInspectorFlipHorizontal, pFlipped => QInspectorOrientation.QInspectorFlipSet(true, pFlipped));
        PInspectorSwitch.PInspectorSwitchAttach(
            pInspectorFlipVertical, pFlipped => QInspectorOrientation.QInspectorFlipSet(false, pFlipped));
    }

    private QInspectorOrientation QInspectorOrientation { get; }

    internal UIElement PInspectorFlipField { get; }

    internal UIElement PInspectorRotateField { get; }

    internal void PInspectorOrientationApply(bool pCapable)
    {
        pInspectorFlipHorizontal.IsChecked = PLook.PLookChecked[QInspectorOrientation.QInspectorFlipHorizontal];
        pInspectorFlipVertical.IsChecked = PLook.PLookChecked[QInspectorOrientation.QInspectorFlipVertical];
        pInspectorRotateCombo.SelectedIndex = QInspectorOrientation.QInspectorRotateIndex;
        pInspectorRotateCombo.IsEnabled = pCapable;
        pInspectorFlipHorizontal.IsEnabled = pCapable;
        pInspectorFlipVertical.IsEnabled = pCapable;
        pInspectorRotateCombo.ToolTip = QInspectorOrientation.QInspectorOrientationTip;
        pInspectorFlipHorizontal.ToolTip = QInspectorOrientation.QInspectorOrientationTip;
        pInspectorFlipVertical.ToolTip = QInspectorOrientation.QInspectorOrientationTip;
    }

    private UIElement PCropFlipBuild()
    {
        var pFlipPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFlipPanel.Children.Add(pInspectorFlipHorizontal);
        pFlipPanel.Children.Add(pInspectorFlipVertical);
        return pFlipPanel;
    }

    private static CheckBox PCropCheckBuild(string pFlipLabel)
    {
        var pFlip = new CheckBox
        {
            Content = pFlipLabel,
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = QPanel.QPanelTextRead(),
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };
        QInteractionClick.QInteractionClickAttach(pFlip);
        QCheckbox.QCheckboxAttach(pFlip);
        return pFlip;
    }

    private ComboBox PInspectorRotateBuild()
    {
        ComboBox pRotateCombo = PCropRotateBuild();
        PCropRotateAttach(pRotateCombo);
        return pRotateCombo;
    }

    private static ComboBox PCropRotateBuild()
    {
        ComboBox pRotateCombo = PInspectorChoice.PInspectorComboBuild();
        pRotateCombo.Items.Add(new LLocalizationChoice("None", "Inspector.Crop.None"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Clockwise90", "Inspector.Crop.Clockwise90"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Degrees180", "Inspector.Crop.Degrees180"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Clockwise270", "Inspector.Crop.Clockwise270"));
        pRotateCombo.SelectedIndex = 0;
        return pRotateCombo;
    }

    private void PCropRotateAttach(ComboBox pRotateCombo) =>
        pRotateCombo.SelectionChanged += (_, _) =>
            QInspectorOrientation.QInspectorRotateSelect(pRotateCombo.SelectedIndex);
}

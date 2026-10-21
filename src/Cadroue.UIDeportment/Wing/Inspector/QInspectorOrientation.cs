using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The orientation tools' wiring: the rotate choice and both flips show the crop gate's turn and hand their raw input
// to it.
public sealed class QInspectorOrientation
{
    private CheckBox qInspectorOrientationHorizontal = null!;
    private ComboBox qInspectorOrientationRotate = null!;
    private CheckBox qInspectorOrientationVertical = null!;
    private readonly QInspector qInspector;
    private readonly CCrop qInspectorOrientationGate;

    public QInspectorOrientation(QInspector lOwner)
    {
        qInspector = lOwner;
        qInspectorOrientationGate = lOwner.QInspectorGate.CInspectorCrop;
    }

    public int QInspectorRotateIndex => qInspectorOrientationGate.CCropRotateIndex;

    public bool QInspectorFlipHorizontal => qInspectorOrientationGate.CCropFlipHorizontal;

    public bool QInspectorFlipVertical => qInspectorOrientationGate.CCropFlipVertical;

    public string? QInspectorOrientationTip => qInspector.QInspectorOrientationCapable
        ? null
        : QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.RequiresTranspose");

    public StackPanel QInspectorOrientationBody { get; private set; } = null!;

    public void QInspectorOrientationBuild()
    {
        QInspectorOrientationBody = QContract.QContractSheetFind<StackPanel>("PInspectorOrientation");
        qInspectorOrientationHorizontal =
            QContract.QContractFind<CheckBox>(QInspectorOrientationBody, "PInspectorHorizontal");
        qInspectorOrientationVertical =
            QContract.QContractFind<CheckBox>(QInspectorOrientationBody, "PInspectorVertical");
        qInspectorOrientationRotate = QContract.QContractFind<ComboBox>(QInspectorOrientationBody, "PInspectorRotate");
        QContract.QContractFind<TextBlock>(QInspectorOrientationBody, "PInspectorFlipLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Flip");
        QContract.QContractFind<TextBlock>(QInspectorOrientationBody, "PInspectorRotateLabel").Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Rotate");
        qInspectorOrientationHorizontal.Content =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Horizontal");
        qInspectorOrientationVertical.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Vertical");
        foreach (string lKey in new[] { "None", "Clockwise90", "Degrees180", "Clockwise270" })
        {
            qInspectorOrientationRotate.Items.Add(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop." + lKey));
        }

        QInteractionClick.QInteractionClickAttach(qInspectorOrientationHorizontal);
        QInteractionClick.QInteractionClickAttach(qInspectorOrientationVertical);
        QCheckbox.QCheckboxAttach(qInspectorOrientationHorizontal);
        QCheckbox.QCheckboxAttach(qInspectorOrientationVertical);
        QDropdown.QDropdownAttach(qInspectorOrientationRotate);
        QInspectorHorizontalAttach();
        QInspectorVerticalAttach();
        QInspectorRotateAttach();
    }

    public void QInspectorOrientationUpdate(bool lCapable)
    {
        QTool.QToolSwitchUpdate(qInspectorOrientationHorizontal, QInspectorFlipHorizontal);
        QTool.QToolSwitchUpdate(qInspectorOrientationVertical, QInspectorFlipVertical);
        qInspectorOrientationRotate.SelectedIndex = QInspectorRotateIndex;
        qInspectorOrientationRotate.IsEnabled = lCapable;
        qInspectorOrientationHorizontal.IsEnabled = lCapable;
        qInspectorOrientationVertical.IsEnabled = lCapable;
        qInspectorOrientationRotate.ToolTip = QInspectorOrientationTip;
        qInspectorOrientationHorizontal.ToolTip = QInspectorOrientationTip;
        qInspectorOrientationVertical.ToolTip = QInspectorOrientationTip;
    }

    public LRotateFlip QInspectorRotateRead() => LRotateFlip.LRotateCropResolve(
        qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop);

    public void QInspectorRotateSelect(int lIndex) => qInspectorOrientationGate.CCropRotateSelect(lIndex);

    public void QInspectorFlipSet(bool lHorizontal, bool lFlipped) =>
        qInspectorOrientationGate.CCropFlipSet(lHorizontal, lFlipped);

    private void QInspectorHorizontalAttach() => QTool.QToolSwitchAttach(
        qInspectorOrientationHorizontal, lValue => QInspectorFlipSet(true, lValue));

    private void QInspectorVerticalAttach() => QTool.QToolSwitchAttach(
        qInspectorOrientationVertical, lValue => QInspectorFlipSet(false, lValue));

    private void QInspectorRotateAttach() => qInspectorOrientationRotate.SelectionChanged += (_, _) =>
        QInspectorRotateSelect(qInspectorOrientationRotate.SelectedIndex);
}

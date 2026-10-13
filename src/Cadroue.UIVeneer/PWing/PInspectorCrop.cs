using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private const string PCropIcon = "/PAsset/PPanel/PProcessingCrop.svg";
    private const double PInspectorInsetWidth = 68;

    private static readonly Brush pInspectorWarnBrush = new SolidColorBrush(Color.FromRgb(0xC2, 0x5A, 0x1E));
    private static readonly Brush pInspectorIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pInspectorAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));

    private static readonly IReadOnlyDictionary<bool, Brush> PInspectorPickBrush = new Dictionary<bool, Brush>
    {
        [true] = pInspectorAccentBrush,
        [false] = pInspectorIconBrush
    };

    private static readonly Brush pInspectorActiveBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0xE3, 0xFA));
    private static readonly Brush pInspectorArmedBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC));

    private static readonly IReadOnlyDictionary<string, Brush> pInspectorToolFaces = new Dictionary<string, Brush>
    {
        ["Active"] = pInspectorActiveBrush,
        ["Armed"] = pInspectorArmedBrush,
        ["Idle"] = Brushes.Transparent
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pInspectorToolInks = new Dictionary<bool, Brush>
    {
        [true] = pInspectorAccentBrush,
        [false] = pInspectorIconBrush
    };

    private TextBox pInspectorInsetLeft = null!;
    private TextBox pInspectorInsetRight = null!;
    private TextBox pInspectorInsetTop = null!;
    private TextBox pInspectorInsetBottom = null!;
    private TextBox pInspectorRatioWidth = null!;
    private TextBox pInspectorRatioHeight = null!;
    private ComboBox pInspectorRatioPreset = null!;
    private StackPanel pInspectorCustomPanel = null!;
    private CheckBox pInspectorRatioFixed = null!;
    private CheckBox pInspectorRatioLenient = null!;
    private TextBlock pInspectorRatioNotice = null!;
    private TextBlock pInspectorResolution = null!;
    private CheckBox pInspectorFlipHorizontal = null!;
    private CheckBox pInspectorFlipVertical = null!;
    private ComboBox pInspectorRotateCombo = null!;
    private ToggleButton pInspectorCropTool = null!;
    private StackPanel pInspectorCropBody = null!;
    private StackPanel pInspectorCropStack = null!;
    private CheckBox pInspectorApplyBox = null!;
    private CheckBox pInspectorPersistentBox = null!;
    private Image pInspectorToolIcon = null!;

    private QInspectorCrop QInspectorCrop => QInspector.QInspectorCrop;

    private void PInspectorCropAttach()
    {
        QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += PInspectorCropUpdate;
        PInspectorCropUpdate();
    }

    private void PInspectorCropUpdate()
    {
        QInspectorCrop lCrop = QInspectorCrop;
        pInspectorApplyBox.IsChecked = PLook.PLookChecked[lCrop.QInspectorActive];
        pInspectorPersistentBox.IsChecked = PLook.PLookChecked[lCrop.QInspectorPersistent];
        pInspectorInsetLeft.Text = lCrop.QInspectorEdgeFormat(0, pInspectorInsetLeft.Text);
        pInspectorInsetTop.Text = lCrop.QInspectorEdgeFormat(1, pInspectorInsetTop.Text);
        pInspectorInsetRight.Text = lCrop.QInspectorEdgeFormat(2, pInspectorInsetRight.Text);
        pInspectorInsetBottom.Text = lCrop.QInspectorEdgeFormat(3, pInspectorInsetBottom.Text);
        pInspectorFlipHorizontal.IsChecked =
            PLook.PLookChecked[QInspector.QInspectorOrientation.QInspectorFlipHorizontal];
        pInspectorFlipVertical.IsChecked = PLook.PLookChecked[QInspector.QInspectorOrientation.QInspectorFlipVertical];
        pInspectorRotateCombo.SelectedIndex = QInspector.QInspectorOrientation.QInspectorRotateIndex;
        pInspectorApplyBox.IsEnabled = QInspector.QInspectorCropCapable;
        pInspectorApplyBox.ToolTip = lCrop.QInspectorApplyTip;
        pInspectorRotateCombo.IsEnabled = QInspector.QInspectorOrientationCapable;
        pInspectorFlipHorizontal.IsEnabled = QInspector.QInspectorOrientationCapable;
        pInspectorFlipVertical.IsEnabled = QInspector.QInspectorOrientationCapable;
        pInspectorRotateCombo.ToolTip = QInspector.QInspectorOrientation.QInspectorOrientationTip;
        pInspectorFlipHorizontal.ToolTip = QInspector.QInspectorOrientation.QInspectorOrientationTip;
        pInspectorFlipVertical.ToolTip = QInspector.QInspectorOrientation.QInspectorOrientationTip;
        PInspectorSectionUpdate(pInspectorCropStack, lCrop.QInspectorCropEnabled);
        PInspectorRatioUpdate();
        PInspectorToolUpdate();
    }

    private void PInspectorRatioUpdate()
    {
        QInspectorCrop lCrop = QInspectorCrop;
        pInspectorRatioPreset.SelectedIndex = lCrop.QInspectorPresetIndex;
        pInspectorCustomPanel.Visibility = PLook.PLookVisible[lCrop.QInspectorCustomShown];
        pInspectorRatioFixed.IsChecked = PLook.PLookChecked[lCrop.QInspectorRatioFixed];
        pInspectorRatioLenient.IsChecked = PLook.PLookChecked[lCrop.QInspectorRatioLenient];
        pInspectorRatioLenient.IsEnabled = lCrop.QInspectorRatioFixed;
        pInspectorResolution.Text = lCrop.QInspectorResolutionRead();
        pInspectorRatioWidth.Text =
            QInspector.QInspectorRatio.QInspectorRatioFormat(true, pInspectorRatioWidth.Text);
        pInspectorRatioHeight.Text =
            QInspector.QInspectorRatio.QInspectorRatioFormat(false, pInspectorRatioHeight.Text);
        pInspectorRatioNotice.Text = lCrop.QInspectorNoticeRead();
        pInspectorRatioNotice.Visibility = PLook.PLookVisible[lCrop.QInspectorNoticeShown];
    }

    private void PInspectorToolUpdate()
    {
        QInspectorCrop lCrop = QInspectorCrop;
        pInspectorCropTool.IsChecked = PLook.PLookChecked[QInspector.QInspectorToolArmed];
        pInspectorCropTool.Background = pInspectorToolFaces[lCrop.QInspectorToolKey];
        pInspectorToolIcon.Source = PIcon.PIconRead(PCropIcon, pInspectorToolInks[lCrop.QInspectorToolActive]);
    }
}

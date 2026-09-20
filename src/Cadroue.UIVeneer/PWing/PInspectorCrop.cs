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

    private LInspectorCrop LInspectorCrop => LInspector.LInspectorCrop;

    private void PInspectorCropAttach()
    {
        LInspectorCrop.LInspectorCropbox.LCropboxStateChange += PInspectorCropUpdate;
        PInspectorCropUpdate();
    }

    private void PInspectorCropUpdate()
    {
        LInspectorCrop lCrop = LInspectorCrop;
        pInspectorApplyBox.IsChecked = PLook.PLookChecked[lCrop.LInspectorActive];
        pInspectorPersistentBox.IsChecked = PLook.PLookChecked[lCrop.LInspectorPersistent];
        pInspectorInsetLeft.Text = lCrop.LInspectorEdgeFormat(0, pInspectorInsetLeft.Text);
        pInspectorInsetTop.Text = lCrop.LInspectorEdgeFormat(1, pInspectorInsetTop.Text);
        pInspectorInsetRight.Text = lCrop.LInspectorEdgeFormat(2, pInspectorInsetRight.Text);
        pInspectorInsetBottom.Text = lCrop.LInspectorEdgeFormat(3, pInspectorInsetBottom.Text);
        pInspectorFlipHorizontal.IsChecked = PLook.PLookChecked[lCrop.LInspectorFlipHorizontal];
        pInspectorFlipVertical.IsChecked = PLook.PLookChecked[lCrop.LInspectorFlipVertical];
        pInspectorRotateCombo.SelectedIndex = lCrop.LInspectorRotateIndex;
        pInspectorApplyBox.IsEnabled = LInspector.LInspectorCropCapable;
        pInspectorApplyBox.ToolTip = lCrop.LInspectorApplyTip;
        pInspectorRotateCombo.IsEnabled = LInspector.LInspectorOrientationCapable;
        pInspectorFlipHorizontal.IsEnabled = LInspector.LInspectorOrientationCapable;
        pInspectorFlipVertical.IsEnabled = LInspector.LInspectorOrientationCapable;
        pInspectorRotateCombo.ToolTip = lCrop.LInspectorOrientationTip;
        pInspectorFlipHorizontal.ToolTip = lCrop.LInspectorOrientationTip;
        pInspectorFlipVertical.ToolTip = lCrop.LInspectorOrientationTip;
        PInspectorSectionUpdate(pInspectorCropStack, lCrop.LInspectorCropEnabled);
        PInspectorRatioUpdate();
        PInspectorToolUpdate();
    }

    private void PInspectorRatioUpdate()
    {
        LInspectorCrop lCrop = LInspectorCrop;
        pInspectorRatioPreset.SelectedIndex = lCrop.LInspectorPresetIndex;
        pInspectorCustomPanel.Visibility = PLook.PLookVisible[lCrop.LInspectorCustomShown];
        pInspectorRatioFixed.IsChecked = PLook.PLookChecked[lCrop.LInspectorRatioFixed];
        pInspectorRatioLenient.IsChecked = PLook.PLookChecked[lCrop.LInspectorRatioLenient];
        pInspectorRatioLenient.IsEnabled = lCrop.LInspectorRatioFixed;
        pInspectorResolution.Text = lCrop.LInspectorResolutionRead();
        pInspectorRatioWidth.Text = lCrop.LInspectorRatioFormat(true, pInspectorRatioWidth.Text);
        pInspectorRatioHeight.Text = lCrop.LInspectorRatioFormat(false, pInspectorRatioHeight.Text);
        pInspectorRatioNotice.Text = lCrop.LInspectorNoticeRead();
        pInspectorRatioNotice.Visibility = PLook.PLookVisible[lCrop.LInspectorNoticeShown];
    }

    private void PInspectorToolUpdate()
    {
        LInspectorCrop lCrop = LInspectorCrop;
        pInspectorCropTool.IsChecked = PLook.PLookChecked[LInspector.LInspectorToolArmed];
        pInspectorCropTool.Background = pInspectorToolFaces[lCrop.LInspectorToolKey];
        pInspectorToolIcon.Source = PIcon.PIconRead(PCropIcon, pInspectorToolInks[lCrop.LInspectorToolActive]);
    }
}

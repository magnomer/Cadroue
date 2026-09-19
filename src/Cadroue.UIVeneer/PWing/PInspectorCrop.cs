using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private const string PCropIcon = "/PAsset/PPanel/PProcessingCrop.svg";
    private const double PInspectorInsetWidth = 68;

    private static readonly Brush pInspectorWarnBrush = new SolidColorBrush(Color.FromRgb(0xC2, 0x5A, 0x1E));
    private static readonly Brush pInspectorIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pInspectorAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));
    private static readonly Brush pInspectorActiveBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0xE3, 0xFA));
    private static readonly Brush pInspectorArmedBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC));

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

    public LCropboxState LCropboxState => LInspector.LInspectorCropbox;

    public event Action<bool>? PInspectorToolChange;
    public event Action? PCropActiveChange;

    private void PInspectorCropAttach()
    {
        LCropboxState.LCropboxStateChange += PInspectorCropUpdate;
        PInspectorCropUpdate();
    }

    public bool PInspectorToolCheck() => LInspector.LInspectorToolArmed;

    public void PInspectorSourceSet(double pSourceWidth, double pSourceHeight) =>
        LInspector.LInspectorSourceSet(pSourceWidth, pSourceHeight);

    public LWorkCrop PInspectorCropRead() => LInspector.LInspectorCropRead();

    public Rect? PInspectorRectRead() =>
        LInspector.LInspectorRectRead() is { } pCropBox
            ? new Rect(pCropBox.LCropboxX, pCropBox.LCropboxY, pCropBox.LCropboxWidth, pCropBox.LCropboxHeight)
            : null;

    public void PCropPlanApply(LWorkCrop pInspectorPlan, bool pInspectorApply) =>
        LInspector.LInspectorCropApply(pInspectorPlan, pInspectorApply);

    public bool PCropPersistentCheck() => LCropboxState.LCropboxStatePersistent;

    public bool PCropActiveCheck() => LCropboxState.LCropboxStateActive;

    public void PCropMediaReset() => LInspector.LInspectorCropReset();

    public void PCropCapabilitySet(bool pCropCapable, bool pOrientationCapable) =>
        LInspector.LInspectorCapableSet(pCropCapable, pOrientationCapable);

    private void PInspectorCropUpdate()
    {
        LWorkCrop pCrop = LCropboxState.LCropboxStateCrop;
        bool pActive = LCropboxState.LCropboxStateActive;
        bool pCapable = LInspector.LInspectorCropCapable;
        bool pOrientationCapable = LInspector.LInspectorOrientationCapable;
        bool pFlipped = (pInspectorApplyBox.IsChecked == true) != pActive;
        PInspectorSwitchUpdate(pInspectorApplyBox, pActive, false);
        PInspectorSwitchUpdate(pInspectorPersistentBox, LCropboxState.LCropboxStatePersistent, true);
        PInspectorWholeSet(pInspectorInsetLeft, pCrop.LWorkCropLeft);
        PInspectorWholeSet(pInspectorInsetTop, pCrop.LWorkCropTop);
        PInspectorWholeSet(pInspectorInsetRight, pCrop.LWorkCropRight);
        PInspectorWholeSet(pInspectorInsetBottom, pCrop.LWorkCropBottom);
        PInspectorSwitchUpdate(pInspectorFlipHorizontal, pCrop.LWorkFlipHorizontal, false);
        PInspectorSwitchUpdate(pInspectorFlipVertical, pCrop.LWorkFlipVertical, false);
        int pRotateIndex = pCrop.LWorkCropRotation switch { 90 => 1, 180 => 2, 270 => 3, _ => 0 };
        if (pInspectorRotateCombo.SelectedIndex != pRotateIndex)
        {
            pInspectorRotateCombo.SelectedIndex = pRotateIndex;
        }

        pInspectorApplyBox.IsEnabled = pCapable;
        pInspectorApplyBox.ToolTip = pCapable
            ? LLocalization.LLocalizationTextRead("Inspector.Crop.ApplyTooltip")
            : LLocalization.LLocalizationTextRead("Inspector.Crop.RequiresCrop");
        pInspectorRotateCombo.IsEnabled = pOrientationCapable;
        pInspectorFlipHorizontal.IsEnabled = pOrientationCapable;
        pInspectorFlipVertical.IsEnabled = pOrientationCapable;
        string? pOrientationNotice = pOrientationCapable
            ? null
            : LLocalization.LLocalizationTextRead("Inspector.Crop.RequiresTranspose");
        pInspectorRotateCombo.ToolTip = pOrientationNotice;
        pInspectorFlipHorizontal.ToolTip = pOrientationNotice;
        pInspectorFlipVertical.ToolTip = pOrientationNotice;
        PInspectorSectionUpdate(pInspectorCropStack, pActive && pCapable);
        if (!pActive)
        {
            LInspector.LInspectorToolSet(false);
        }

        PInspectorRatioUpdate();
        PInspectorToolUpdate();
        if (pFlipped)
        {
            PCropActiveChange?.Invoke();
        }
    }

    private void PInspectorToolUpdate()
    {
        bool pToolArmed = LInspector.LInspectorToolArmed;
        bool pToolActive = pToolArmed && PInspectorRectRead() is not null;
        bool pWasArmed = pInspectorCropTool.IsChecked == true;
        pInspectorCropTool.IsChecked = pToolArmed;
        pInspectorCropTool.Background = pToolActive
            ? pInspectorActiveBrush
            : pToolArmed ? pInspectorArmedBrush : Brushes.Transparent;

        pInspectorToolIcon.Source = PIcon.PIconRead(
            PCropIcon,
            pToolActive ? pInspectorAccentBrush : pInspectorIconBrush);
        if (pWasArmed != pToolArmed)
        {
            PInspectorToolChange?.Invoke(pToolArmed);
        }
    }
}

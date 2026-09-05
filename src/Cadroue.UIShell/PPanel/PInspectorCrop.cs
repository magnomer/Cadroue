using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PAsset;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private const string PCropIcon = "/PAsset/PPanel/PProcessingCrop.svg";
    private const double PInspectorInsetWidth = 68;
    private const double PInspectorRatioTolerance = 0.01;

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

    private double pInspectorSourceWidth = 1920;
    private double pInspectorSourceHeight = 1080;
    private bool pInspectorSourcePresent;
    private bool pInspectorCropSuppress;
    private bool pInspectorRatioSuppress;
    private bool pInspectorCropPresent;
    private bool pInspectorCropCapable = true;
    private readonly bool[] pInspectorEdgeLocked = new bool[4];

    public event Action<bool>? PInspectorToolChange;

    public bool PInspectorToolCheck() => pInspectorCropTool.IsChecked == true;
    public event Action<Size?>? PInspectorRatioChange;
    public event Action<Rect?>? PInspectorCropChange;
    public event Action<LRotateFlip>? PInspectorRotateChange;
    public event Action<bool>? PInspectorPersistentChange;
    public event Action? PCropActiveChange;

    public void PInspectorSourceSet(double pSourceWidth, double pSourceHeight)
    {
        pInspectorSourcePresent = pSourceWidth > 0 && pSourceHeight > 0;
        pInspectorSourceWidth = pInspectorSourcePresent ? pSourceWidth : 0;
        pInspectorSourceHeight = pInspectorSourcePresent ? pSourceHeight : 0;
        PInspectorRatioUpdate();
    }

    public LWorkCrop PInspectorCropRead() => PInspectorCanonicalRead();

    private LWorkCrop PInspectorCanonicalRead() => LCropbox.LCropboxEdgeNormalize(
        new LWorkCrop(
            PInspectorWholeRead(pInspectorInsetLeft),
            PInspectorWholeRead(pInspectorInsetTop),
            PInspectorWholeRead(pInspectorInsetRight),
            PInspectorWholeRead(pInspectorInsetBottom),
            PInspectorAngleResolve(PInspectorKindRead()),
            pInspectorFlipHorizontal.IsChecked == true,
            pInspectorFlipVertical.IsChecked == true),
        pInspectorSourceWidth,
        pInspectorSourceHeight);

    public void PCropPlanApply(LWorkCrop pInspectorPlan, bool pInspectorApply)
    {
        PInspectorEdgeClear();
        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        try
        {
            pInspectorApplyBox.IsChecked = pInspectorApply;
            pInspectorInsetLeft.Text = pInspectorPlan.LWorkCropLeft.ToString();
            pInspectorInsetTop.Text = pInspectorPlan.LWorkCropTop.ToString();
            pInspectorInsetRight.Text = pInspectorPlan.LWorkCropRight.ToString();
            pInspectorInsetBottom.Text = pInspectorPlan.LWorkCropBottom.ToString();
            pInspectorFlipHorizontal.IsChecked = pInspectorPlan.LWorkFlipHorizontal;
            pInspectorFlipVertical.IsChecked = pInspectorPlan.LWorkFlipVertical;
            pInspectorRotateCombo.SelectedIndex = pInspectorPlan.LWorkCropRotation switch
            {
                90 => 1,
                180 => 2,
                270 => 3,
                _ => 0
            };
            pInspectorCropPresent = pInspectorPlan.LWorkEdgeActive;
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorRotateRaise();
        PInspectorCropRaise();
        PInspectorRatioUpdate();
        PInspectorToolUpdate();
        PInspectorApplyUpdate();
    }

    public bool PCropPersistentCheck() => pInspectorPersistentBox.IsChecked == true;

    public bool PCropActiveCheck() => pInspectorApplyBox.IsChecked == true;

    public void PCropMediaReset()
    {
        if (pInspectorPersistentBox.IsChecked == true)
        {
            return;
        }

        PInspectorCropReset();
    }

    private void PInspectorCropReset()
    {
        PInspectorEdgeClear();
        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        try
        {
            pInspectorApplyBox.IsChecked = false;
            pInspectorCropTool.IsChecked = false;
            pInspectorFlipHorizontal.IsChecked = false;
            pInspectorFlipVertical.IsChecked = false;
            pInspectorRotateCombo.SelectedIndex = 0;
            pInspectorRatioFixed.IsChecked = false;
            pInspectorRatioLenient.IsChecked = false;
            pInspectorRatioLenient.IsEnabled = false;
            pInspectorRatioPreset.SelectedIndex = 0;
            pInspectorInsetLeft.Text = "0";
            pInspectorInsetTop.Text = "0";
            pInspectorInsetRight.Text = "0";
            pInspectorInsetBottom.Text = "0";
            pInspectorRatioWidth.Text = "0";
            pInspectorRatioHeight.Text = "0";
            pInspectorCropPresent = false;
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorToolChange?.Invoke(false);
        PInspectorRatioChange?.Invoke(null);
        PInspectorRotateRaise();
        PInspectorCropChange?.Invoke(null);
        PInspectorRatioUpdate();
        PInspectorToolUpdate();
        PInspectorApplyUpdate();
    }

    private void PInspectorPersistentRaise()
    {
        PInspectorPersistentChange?.Invoke(pInspectorPersistentBox.IsChecked == true);
    }

    public void PCropCapabilitySet(bool pCropCapable, bool pOrientationCapable)
    {
        pInspectorCropCapable = pCropCapable;
        pInspectorApplyBox.IsEnabled = pCropCapable;
        pInspectorApplyBox.ToolTip = pCropCapable
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
        PCropEnableApply();
    }

    private void PCropEnableApply()
    {
        bool pApplyActive = pInspectorApplyBox.IsChecked == true && pInspectorCropCapable;
        pInspectorCropStack.IsEnabled = pApplyActive;
        pInspectorCropStack.Opacity = pApplyActive ? 1 : 0.4;
    }

    private void PInspectorApplyUpdate()
    {
        if (pInspectorApplyBox.IsChecked != true && pInspectorCropTool.IsChecked == true)
        {
            pInspectorCropTool.IsChecked = false;
        }

        PCropEnableApply();
        PCropActiveChange?.Invoke();
    }

    private void PInspectorToolUpdate()
    {
        bool pToolArmed = pInspectorCropTool.IsChecked == true;
        bool pToolActive = pToolArmed && pInspectorCropPresent;

        pInspectorCropTool.Background = pToolActive
            ? pInspectorActiveBrush
            : pToolArmed ? pInspectorArmedBrush : Brushes.Transparent;

        pInspectorToolIcon.Source = PIcon.PIconRead(
            PCropIcon,
            pToolActive ? pInspectorAccentBrush : pInspectorIconBrush);
    }

    private void PInspectorToolReset()
    {
        pInspectorCropSuppress = true;
        try
        {
            pInspectorCropTool.IsChecked = false;
        }
        finally
        {
            pInspectorCropSuppress = false;
        }
    }
}

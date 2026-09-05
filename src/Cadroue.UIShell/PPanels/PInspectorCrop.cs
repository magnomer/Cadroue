using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const string PCropIcon = "/PAssets/PPanels/PProcessingCrop.svg";
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

    private static int PInspectorWholeRead(TextBox pNumberBox) =>
        (int)Math.Round(PInspectorNumberRead(pNumberBox));

    private void PInspectorInsetChange(int pEdge)
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        TextBox pEdgeBox = pEdge switch
        {
            0 => pInspectorInsetLeft,
            2 => pInspectorInsetRight,
            1 => pInspectorInsetTop,
            _ => pInspectorInsetBottom
        };
        pInspectorEdgeLocked[pEdge] = !string.IsNullOrWhiteSpace(pEdgeBox.Text);

        if (pInspectorRatioFixed.IsChecked == true && pInspectorSourcePresent && !pInspectorRatioSuppress)
        {
            PInspectorRatioResolve(pEdge);
        }

        PInspectorRatioUpdate();
        PInspectorCropRaise();
    }

    private void PInspectorEdgeClear() => Array.Clear(pInspectorEdgeLocked);

    public void PInspectorCropSet(Rect? pCropVideo, int pDriveAxis, int pAnchorX, int pAnchorY)
    {
        PInspectorEdgeClear();
        Rect? pCropSnapped = pCropVideo is { Width: > 0, Height: > 0 } pCropDrawn
            ? PInspectorRatioResolve(pCropDrawn, pDriveAxis, pAnchorX, pAnchorY) ?? pCropDrawn
            : pCropVideo;

        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        pInspectorCropPresent = pCropSnapped is { Width: > 0, Height: > 0 };
        PInspectorToolUpdate();
        try
        {
            if (pCropSnapped is not { Width: > 0, Height: > 0 } pCropRect)
            {
                pInspectorInsetLeft.Text = "0";
                pInspectorInsetTop.Text = "0";
                pInspectorInsetRight.Text = "0";
                pInspectorInsetBottom.Text = "0";
            }
            else
            {
                pInspectorInsetLeft.Text = PInspectorEdgeFormat(pCropRect.X);
                pInspectorInsetTop.Text = PInspectorEdgeFormat(pCropRect.Y);
                pInspectorInsetRight.Text = PInspectorEdgeFormat(pInspectorSourceWidth - pCropRect.X - pCropRect.Width);
                pInspectorInsetBottom.Text = PInspectorEdgeFormat(pInspectorSourceHeight - pCropRect.Y - pCropRect.Height);
            }

            PInspectorRatioUpdate();
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorCropRaise();
    }

    public LRotateFlip PInspectorRotateRead() => new(
        PInspectorKindRead(),
        pInspectorFlipHorizontal.IsChecked == true,
        pInspectorFlipVertical.IsChecked == true);

    public void PInspectorOrientationApply(LRotateFlip pInspectorOld)
    {
        var pInspectorSource = new LWorkCrop(
            (int)Math.Round(PInspectorNumberRead(pInspectorInsetLeft)),
            (int)Math.Round(PInspectorNumberRead(pInspectorInsetTop)),
            (int)Math.Round(PInspectorNumberRead(pInspectorInsetRight)),
            (int)Math.Round(PInspectorNumberRead(pInspectorInsetBottom)),
            PInspectorAngleResolve(pInspectorOld.LRotateKind),
            pInspectorOld.LRotateFlipHorizontal,
            pInspectorOld.LRotateFlipVertical);

        if (!pInspectorSource.LWorkEdgeActive)
        {
            return;
        }

        LRotateFlip pInspectorNew = PInspectorRotateRead();
        LWorkCrop pInspectorMapped = LCropbox.LCropboxOrientationResolve(
            pInspectorSource,
            PInspectorAngleResolve(pInspectorNew.LRotateKind),
            pInspectorNew.LRotateFlipHorizontal,
            pInspectorNew.LRotateFlipVertical);

        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        try
        {
            pInspectorInsetLeft.Text = pInspectorMapped.LWorkCropLeft.ToString(CultureInfo.InvariantCulture);
            pInspectorInsetTop.Text = pInspectorMapped.LWorkCropTop.ToString(CultureInfo.InvariantCulture);
            pInspectorInsetRight.Text = pInspectorMapped.LWorkCropRight.ToString(CultureInfo.InvariantCulture);
            pInspectorInsetBottom.Text = pInspectorMapped.LWorkCropBottom.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorCropRaise();
    }

    private static int PInspectorAngleResolve(LRotateKind pInspectorKind) => pInspectorKind switch
    {
        LRotateKind.LRotate90 => 90,
        LRotateKind.LRotate180 => 180,
        LRotateKind.LRotate270 => 270,
        _ => 0
    };

    public Rect? PInspectorRectRead() => PInspectorRectResolve();

    private Rect? PInspectorRectResolve() =>
        LCropbox.LCropboxRectResolve(PInspectorCanonicalRead(), pInspectorSourceWidth, pInspectorSourceHeight)
            is { } pCropBox
            ? new Rect(pCropBox.LCropboxX, pCropBox.LCropboxY, pCropBox.LCropboxWidth, pCropBox.LCropboxHeight)
            : null;

    private void PInspectorRotateRaise()
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        PInspectorRotateChange?.Invoke(new LRotateFlip(
            PInspectorKindRead(),
            pInspectorFlipHorizontal.IsChecked == true,
            pInspectorFlipVertical.IsChecked == true));
    }

    private LRotateKind PInspectorKindRead() => pInspectorRotateCombo.SelectedIndex switch
    {
        1 => LRotateKind.LRotate90,
        2 => LRotateKind.LRotate180,
        3 => LRotateKind.LRotate270,
        _ => LRotateKind.LRotateNone
    };

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

    private void PInspectorCropClear()
    {
        if (pInspectorCropSuppress || !pInspectorCropPresent)
        {
            return;
        }

        PInspectorCropChange?.Invoke(null);
    }

    private void PInspectorEdgesReset()
    {
        PInspectorEdgeClear();
        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        try
        {
            pInspectorInsetLeft.Text = "0";
            pInspectorInsetTop.Text = "0";
            pInspectorInsetRight.Text = "0";
            pInspectorInsetBottom.Text = "0";
            pInspectorCropPresent = false;
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorCropChange?.Invoke(null);
        PInspectorRatioUpdate();
        PInspectorToolUpdate();
    }

    private void PInspectorCropRaise()
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        Rect? pCropRect = PInspectorRectResolve();
        pInspectorCropPresent = pCropRect is not null;
        PInspectorToolUpdate();
        PInspectorCropChange?.Invoke(pCropRect);
    }

    private static string PInspectorEdgeFormat(double pEdgeValue) =>
        Math.Max(0, Math.Round(pEdgeValue)).ToString(CultureInfo.InvariantCulture);

    private static double PInspectorNumberRead(TextBox pNumberBox) =>
        double.TryParse(pNumberBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out double pNumber)
            ? pNumber
            : 0;

    public (bool RatioFixed, bool RatioLenient, int RatioWidth, int RatioHeight) PInspectorRatioRead() => (
        pInspectorRatioFixed.IsChecked == true,
        pInspectorRatioFixed.IsChecked == true && pInspectorRatioLenient.IsChecked == true,
        (int)Math.Round(PInspectorNumberRead(pInspectorRatioWidth)),
        (int)Math.Round(PInspectorNumberRead(pInspectorRatioHeight)));

    public void PInspectorRatioApply(bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight)
    {
        bool pCropSuppressPrevious = pInspectorCropSuppress;
        bool pRatioSuppressPrevious = pInspectorRatioSuppress;
        pInspectorCropSuppress = true;
        pInspectorRatioSuppress = true;
        try
        {
            int pPresetIndex = PInspectorPresetResolve(pRatioWidth, pRatioHeight);
            pInspectorRatioPreset.SelectedIndex = pPresetIndex;
            pInspectorCustomPanel.Visibility = pPresetIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            pInspectorRatioWidth.Text = pRatioWidth.ToString(CultureInfo.InvariantCulture);
            pInspectorRatioHeight.Text = pRatioHeight.ToString(CultureInfo.InvariantCulture);
            pInspectorRatioFixed.IsChecked = pRatioFixed && pRatioWidth > 0 && pRatioHeight > 0;
            pInspectorRatioLenient.IsChecked = pRatioLenient && pRatioFixed && pRatioWidth > 0 && pRatioHeight > 0;
            pInspectorRatioLenient.IsEnabled = pInspectorRatioFixed.IsChecked == true;
        }
        finally
        {
            pInspectorRatioSuppress = pRatioSuppressPrevious;
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorRatioRaise();
        PInspectorRatioUpdate();
    }

    public void PInspectorRatioReset()
    {
        PInspectorEdgeClear();
        bool pCropSuppressPrevious = pInspectorCropSuppress;
        bool pRatioSuppressPrevious = pInspectorRatioSuppress;
        pInspectorCropSuppress = true;
        pInspectorRatioSuppress = true;
        try
        {
            pInspectorRatioFixed.IsChecked = false;
            pInspectorRatioLenient.IsChecked = false;
            pInspectorRatioLenient.IsEnabled = false;
            pInspectorRatioPreset.SelectedIndex = 0;
            pInspectorCustomPanel.Visibility = Visibility.Visible;
            pInspectorRatioWidth.Text = "0";
            pInspectorRatioHeight.Text = "0";
        }
        finally
        {
            pInspectorRatioSuppress = pRatioSuppressPrevious;
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorRatioChange?.Invoke(null);
        PInspectorRatioUpdate();
    }

    private static int PInspectorPresetResolve(int pRatioWidth, int pRatioHeight) =>
        pRatioWidth <= 0 || pRatioHeight <= 0
            ? 0
            : (pRatioWidth, pRatioHeight) switch
            {
                (16, 9) => 1,
                (9, 16) => 2,
                (4, 3) => 3,
                (3, 4) => 4,
                (1, 1) => 5,
                (21, 9) => 6,
                _ => 0
            };
}

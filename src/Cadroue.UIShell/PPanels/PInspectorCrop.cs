using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const string PCropIcon = "/PAssets/PPanels/PProcessingCrop.svg";
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
    private CheckBox pInspectorRatioFixed = null!;
    private TextBlock pInspectorRatioNotice = null!;
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
    private bool pInspectorCropSuppress;
    private bool pInspectorRatioSuppress;
    private bool pInspectorCropPresent;

    public event Action<bool>? PInspectorToolChange;

    public bool PInspectorToolCheck() => pInspectorCropTool.IsChecked == true;
    public event Action<Size?>? PInspectorRatioChange;
    public event Action<Rect?>? PInspectorCropChange;
    public event Action<LRotateFlip>? PInspectorRotateChange;
    public event Action<bool>? PInspectorPersistentChange;
    public event Action? PCropActiveChange;

    public void PInspectorSourceSet(double pSourceWidth, double pSourceHeight)
    {
        pInspectorSourceWidth = pSourceWidth;
        pInspectorSourceHeight = pSourceHeight;
        PInspectorRatioUpdate();
    }

    public LWorkCrop PInspectorCropRead()
    {
        if (pInspectorApplyBox.IsChecked != true)
        {
            return LWorkCrop.LWorkCropCreate();
        }

        return new LWorkCrop(
            PInspectorEvenClamp(pInspectorInsetLeft),
            PInspectorEvenClamp(pInspectorInsetTop),
            PInspectorEvenClamp(pInspectorInsetRight),
            PInspectorEvenClamp(pInspectorInsetBottom),
            PInspectorKindRead() switch
            {
                LRotateKind.LRotate90 => 90,
                LRotateKind.LRotate180 => 180,
                LRotateKind.LRotate270 => 270,
                _ => 0
            },
            pInspectorFlipHorizontal.IsChecked == true,
            pInspectorFlipVertical.IsChecked == true);
    }

    public void PCropPlanApply(LWorkCrop pInspectorPlan, bool pInspectorApply)
    {
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
        if (pInspectorPlan.LWorkEdgeActive)
        {
            PInspectorCropRaise();
        }
        else
        {
            pInspectorCropPresent = false;
            PInspectorCropChange?.Invoke(null);
        }

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

    private void PInspectorApplyUpdate()
    {
        bool pApplyActive = pInspectorApplyBox.IsChecked == true;
        pInspectorCropStack.IsEnabled = pApplyActive;
        pInspectorCropStack.Opacity = pApplyActive ? 1 : 0.4;
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

    private static int PInspectorEvenClamp(TextBox pNumberBox)
    {
        int pWhole = (int)Math.Round(PInspectorNumberRead(pNumberBox));
        return pWhole <= 0 ? 0 : pWhole - (pWhole % 2);
    }

    public void PInspectorCropSet(Rect? pCropVideo)
    {
        Rect? pCropSnapped = pCropVideo is { Width: > 0, Height: > 0 } pCropDrawn
            ? PInspectorRatioClamp(pCropDrawn) ?? pCropDrawn
            : pCropVideo;
        bool pCropAdjusted = pCropSnapped != pCropVideo;

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

        if (pCropAdjusted)
        {
            PInspectorCropChange?.Invoke(pCropSnapped);
        }
    }

    public LRotateFlip PInspectorRotateRead() => new(
        PInspectorKindRead(),
        pInspectorFlipHorizontal.IsChecked == true,
        pInspectorFlipVertical.IsChecked == true);

    public Rect? PInspectorRectRead()
    {
        if (pInspectorApplyBox.IsChecked != true)
        {
            return null;
        }

        double pCropLeft = PInspectorNumberRead(pInspectorInsetLeft);
        double pCropTop = PInspectorNumberRead(pInspectorInsetTop);
        double pCropWidth = pInspectorSourceWidth - pCropLeft - PInspectorNumberRead(pInspectorInsetRight);
        double pCropHeight = pInspectorSourceHeight - pCropTop - PInspectorNumberRead(pInspectorInsetBottom);
        bool pCropEdged = pCropLeft > 0
            || pCropTop > 0
            || PInspectorNumberRead(pInspectorInsetRight) > 0
            || PInspectorNumberRead(pInspectorInsetBottom) > 0;

        return pCropEdged && pCropWidth > 0 && pCropHeight > 0
            ? new Rect(pCropLeft, pCropTop, pCropWidth, pCropHeight)
            : null;
    }

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

    private void PInspectorCropRaise()
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        double pCropLeft = PInspectorNumberRead(pInspectorInsetLeft);
        double pCropTop = PInspectorNumberRead(pInspectorInsetTop);
        double pCropWidth = pInspectorSourceWidth - pCropLeft - PInspectorNumberRead(pInspectorInsetRight);
        double pCropHeight = pInspectorSourceHeight - pCropTop - PInspectorNumberRead(pInspectorInsetBottom);
        pInspectorCropPresent = pCropWidth > 0 && pCropHeight > 0;
        PInspectorToolUpdate();
        PInspectorCropChange?.Invoke(pInspectorCropPresent
            ? new Rect(pCropLeft, pCropTop, pCropWidth, pCropHeight)
            : null);
    }

    private static string PInspectorEdgeFormat(double pEdgeValue) =>
        Math.Max(0, Math.Round(pEdgeValue)).ToString(CultureInfo.InvariantCulture);

    private static double PInspectorNumberRead(TextBox pNumberBox) =>
        double.TryParse(pNumberBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out double pNumber)
            ? pNumber
            : 0;
}

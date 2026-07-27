using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const string PInspectorCropIconPath = "/PAssets/PPanels/PProcessingCrop.svg";
    private const double PInspectorInsetWidth = 68;

    private static readonly Brush pInspectorWarnBrush = new SolidColorBrush(Color.FromRgb(0xC2, 0x5A, 0x1E));
    private static readonly Brush pInspectorIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

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

    private double pInspectorSourceWidth = 1920;
    private double pInspectorSourceHeight = 1080;
    private bool pInspectorCropSuppress;
    private bool pInspectorCropPresent;

    public event Action<bool>? PInspectorToolChange;
    public event Action<Size?>? PInspectorRatioChange;
    public event Action<Rect?>? PInspectorCropChange;
    public event Action<LRotateFlip>? PInspectorRotateChange;

    public void PInspectorSourceSet(double pSourceWidth, double pSourceHeight)
    {
        pInspectorSourceWidth = pSourceWidth;
        pInspectorSourceHeight = pSourceHeight;
        PInspectorRatioUpdate();
    }

    public LWorkCrop PInspectorCropRead() => new(
        PInspectorEvenClamp(pInspectorInsetLeft),
        PInspectorEvenClamp(pInspectorInsetTop),
        PInspectorEvenClamp(pInspectorInsetRight),
        PInspectorEvenClamp(pInspectorInsetBottom),
        PInspectorRotateKindRead() switch
        {
            LRotateKind.LRotate90 => 90,
            LRotateKind.LRotate180 => 180,
            LRotateKind.LRotate270 => 270,
            _ => 0
        },
        pInspectorFlipHorizontal.IsChecked == true,
        pInspectorFlipVertical.IsChecked == true);

    private static int PInspectorEvenClamp(TextBox pNumberBox)
    {
        int pWhole = (int)Math.Round(PInspectorNumberRead(pNumberBox));
        return pWhole <= 0 ? 0 : pWhole - (pWhole % 2);
    }

    public void PInspectorCropSet(Rect? pCropVideo)
    {
        Rect? pCropSnapped = pCropVideo is { Width: > 0, Height: > 0 } pCropDrawn
            ? PInspectorRatioSnap(pCropDrawn) ?? pCropDrawn
            : pCropVideo;
        bool pCropAdjusted = pCropSnapped != pCropVideo;

        pInspectorCropSuppress = true;
        pInspectorCropPresent = pCropSnapped is { Width: > 0, Height: > 0 };
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
            pInspectorCropSuppress = false;
        }

        if (pCropAdjusted)
        {
            PInspectorCropChange?.Invoke(pCropSnapped);
        }
    }

    private Rect? PInspectorRatioSnap(Rect pCropRect)
    {
        if (pInspectorRatioFixed.IsChecked != true)
        {
            return null;
        }

        int pRatioWidth = (int)Math.Round(PInspectorNumberRead(pInspectorRatioWidth));
        int pRatioHeight = (int)Math.Round(PInspectorNumberRead(pInspectorRatioHeight));
        if (pRatioWidth <= 0 || pRatioHeight <= 0)
        {
            return null;
        }

        int pDivisor = PInspectorDivisorRead(pRatioWidth, pRatioHeight);
        int pUnitWidth = pRatioWidth / pDivisor;
        int pUnitHeight = pRatioHeight / pDivisor;

        int pScale = (int)Math.Floor(Math.Min(
            pCropRect.Width / pUnitWidth,
            pCropRect.Height / pUnitHeight));

        while (pScale > 0 && ((pScale * pUnitWidth % 2) != 0 || (pScale * pUnitHeight % 2) != 0))
        {
            pScale--;
        }

        if (pScale <= 0)
        {
            return null;
        }

        double pSnapWidth = pScale * pUnitWidth;
        double pSnapHeight = pScale * pUnitHeight;
        double pSnapX = PInspectorEvenFloor(pCropRect.X + ((pCropRect.Width - pSnapWidth) / 2));
        double pSnapY = PInspectorEvenFloor(pCropRect.Y + ((pCropRect.Height - pSnapHeight) / 2));
        pSnapX = Math.Clamp(pSnapX, 0, Math.Max(0, PInspectorEvenFloor(pInspectorSourceWidth - pSnapWidth)));
        pSnapY = Math.Clamp(pSnapY, 0, Math.Max(0, PInspectorEvenFloor(pInspectorSourceHeight - pSnapHeight)));
        return new Rect(pSnapX, pSnapY, pSnapWidth, pSnapHeight);
    }

    private static double PInspectorEvenFloor(double pValue)
    {
        int pWhole = (int)Math.Floor(pValue);
        return pWhole <= 0 ? 0 : pWhole - (pWhole % 2);
    }

    private StackPanel PInspectorCropBodyBuild()
    {
        pInspectorInsetLeft = PInspectorInsetBuild();
        pInspectorInsetRight = PInspectorInsetBuild();
        pInspectorInsetTop = PInspectorInsetBuild();
        pInspectorInsetBottom = PInspectorInsetBuild();
        pInspectorRatioWidth = PInspectorRatioFieldBuild();
        pInspectorRatioHeight = PInspectorRatioFieldBuild();

        pInspectorRatioFixed = new CheckBox
        {
            Content = "Fixed ratio",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(PInspectorLabelWidth, 8, 0, 0)
        };
        pInspectorRatioFixed.Checked += (_, _) => PInspectorRatioCommit();
        pInspectorRatioFixed.Unchecked += (_, _) => PInspectorRatioCommit();

        pInspectorRatioNotice = new TextBlock
        {
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorWarnBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        pInspectorFlipHorizontal = PInspectorFlipBuild("Horizontal");
        pInspectorFlipVertical = PInspectorFlipBuild("Vertical");
        pInspectorFlipHorizontal.Checked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipHorizontal.Unchecked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipVertical.Checked += (_, _) => PInspectorRotateRaise();
        pInspectorFlipVertical.Unchecked += (_, _) => PInspectorRotateRaise();
        pInspectorRotateCombo = PInspectorRotateBuild();
        pInspectorCropTool = PInspectorToolBuild();

        pInspectorCropBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorCropBody.Children.Add(PInspectorFieldBuild("Tool", pInspectorCropTool));
        pInspectorCropBody.Children.Add(PInspectorFieldBuild("Flip", PInspectorFlipRowBuild()));
        pInspectorCropBody.Children.Add(PInspectorFieldBuild("Rotate", pInspectorRotateCombo));
        pInspectorCropBody.Children.Add(PInspectorEdgeBuild());
        pInspectorCropBody.Children.Add(PInspectorRatioBuild());
        pInspectorCropBody.Children.Add(pInspectorRatioFixed);
        pInspectorCropBody.Children.Add(pInspectorRatioNotice);
        return pInspectorCropBody;
    }

    private ToggleButton PInspectorToolBuild()
    {
        var pToolButton = new ToggleButton
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(PInspectorCropIconPath, pInspectorIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = "Draw the crop box on the preview",
            Width = 28,
            Height = 26,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonToggleCreate()
        };
        pToolButton.Checked += (_, _) => PInspectorToolChange?.Invoke(true);
        pToolButton.Unchecked += (_, _) =>
        {
            PInspectorToolChange?.Invoke(false);
            PInspectorCropClear();
        };
        return pToolButton;
    }

    private ComboBox PInspectorRotateBuild()
    {
        var pRotateCombo = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pRotateCombo);
        pRotateCombo.Items.Add("None");
        pRotateCombo.Items.Add("90° clockwise");
        pRotateCombo.Items.Add("180°");
        pRotateCombo.Items.Add("270° clockwise");
        pRotateCombo.SelectedIndex = 0;
        pRotateCombo.SelectionChanged += (_, _) =>
        {
            PInspectorRatioUpdate();
            PInspectorRotateRaise();
        };
        return pRotateCombo;
    }

    private void PInspectorRotateRaise()
    {
        PInspectorRotateChange?.Invoke(new LRotateFlip(
            PInspectorRotateKindRead(),
            pInspectorFlipHorizontal.IsChecked == true,
            pInspectorFlipVertical.IsChecked == true));
    }

    private LRotateKind PInspectorRotateKindRead() => pInspectorRotateCombo.SelectedIndex switch
    {
        1 => LRotateKind.LRotate90,
        2 => LRotateKind.LRotate180,
        3 => LRotateKind.LRotate270,
        _ => LRotateKind.LRotateNone
    };

    private UIElement PInspectorEdgeBuild()
    {
        var pCropGrid = new Grid { Margin = new Thickness(0, 14, 0, 4) };
        for (int pColumn = 0; pColumn < 3; pColumn++)
        {
            pCropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (int pRow = 0; pRow < 3; pRow++)
        {
            pCropGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        PInspectorCellAdd(pCropGrid, PInspectorCellBuild("Top", pInspectorInsetTop), 0, 1);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild("Left", pInspectorInsetLeft), 1, 0);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild("Right", pInspectorInsetRight), 1, 2);
        PInspectorCellAdd(pCropGrid, PInspectorCellBuild("Bottom", pInspectorInsetBottom), 2, 1);
        return pCropGrid;
    }

    private static void PInspectorCellAdd(Grid pCropGrid, UIElement pCell, int pRow, int pColumn)
    {
        Grid.SetRow(pCell, pRow);
        Grid.SetColumn(pCell, pColumn);
        pCropGrid.Children.Add(pCell);
    }

    private static UIElement PInspectorCellBuild(string pCellLabel, TextBox pCellBox)
    {
        var pCellPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 3)
        };
        pCellPanel.Children.Add(new TextBlock
        {
            Text = pCellLabel,
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 3)
        });
        pCellPanel.Children.Add(pCellBox);
        return pCellPanel;
    }

    private UIElement PInspectorRatioBuild()
    {
        var pRatioPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 14, 0, 0)
        };
        pRatioPanel.Children.Add(PInspectorLabelBuild("Ratio"));
        pRatioPanel.Children.Add(pInspectorRatioWidth);
        pRatioPanel.Children.Add(new TextBlock
        {
            Text = "×",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, 0, 7, 0)
        });
        pRatioPanel.Children.Add(pInspectorRatioHeight);
        return pRatioPanel;
    }

    private UIElement PInspectorFlipRowBuild()
    {
        var pFlipPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFlipPanel.Children.Add(pInspectorFlipHorizontal);
        pFlipPanel.Children.Add(pInspectorFlipVertical);
        return pFlipPanel;
    }

    private static CheckBox PInspectorFlipBuild(string pFlipLabel) => new()
    {
        Content = pFlipLabel,
        FontSize = 12,
        FontFamily = pInspectorFontFamily,
        Foreground = PPanelTextBrush,
        VerticalAlignment = VerticalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 14, 0)
    };

    private TextBox PInspectorRatioFieldBuild()
    {
        TextBox pRatioBox = PInspectorNumberBoxBuild();
        pRatioBox.TextChanged += (_, _) => PInspectorRatioEditHandle();
        return pRatioBox;
    }

    private void PInspectorRatioEditHandle()
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        PInspectorCropClear();
        PInspectorRatioRaise();
        PInspectorRatioUpdate();
    }

    private void PInspectorToolDisarm()
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

    private TextBox PInspectorInsetBuild()
    {
        TextBox pInsetBox = PInspectorNumberBoxBuild();
        pInsetBox.TextChanged += (_, _) =>
        {
            PInspectorRatioUpdate();
            PInspectorCropRaise();
        };
        return pInsetBox;
    }

    private static TextBox PInspectorNumberBoxBuild()
    {
        var pNumberBox = new TextBox
        {
            Text = "0",
            Width = PInspectorInsetWidth,
            Height = PInspectorFieldHeight,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PTextbox.PTextboxApply(pNumberBox);
        pNumberBox.TextAlignment = TextAlignment.Center;
        pNumberBox.Padding = new Thickness(4, 0, 4, 0);
        pNumberBox.PreviewTextInput += (_, pNumberEvent) =>
            pNumberEvent.Handled = !pNumberEvent.Text.All(char.IsDigit);
        return pNumberBox;
    }

    private void PInspectorRatioCommit()
    {
        PInspectorRatioUpdate();
        PInspectorRatioRaise();
    }

    private void PInspectorRatioRaise()
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        if (pInspectorRatioFixed.IsChecked != true)
        {
            PInspectorRatioChange?.Invoke(null);
            return;
        }

        double pRatioWidth = PInspectorNumberRead(pInspectorRatioWidth);
        double pRatioHeight = PInspectorNumberRead(pInspectorRatioHeight);
        PInspectorRatioChange?.Invoke(pRatioWidth > 0 && pRatioHeight > 0
            ? new Size(pRatioWidth, pRatioHeight)
            : null);
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
        PInspectorCropChange?.Invoke(pInspectorCropPresent
            ? new Rect(pCropLeft, pCropTop, pCropWidth, pCropHeight)
            : null);
    }

    private void PInspectorRatioUpdate()
    {
        double pCropWidth = pInspectorSourceWidth
            - PInspectorNumberRead(pInspectorInsetLeft)
            - PInspectorNumberRead(pInspectorInsetRight);
        double pCropHeight = pInspectorSourceHeight
            - PInspectorNumberRead(pInspectorInsetTop)
            - PInspectorNumberRead(pInspectorInsetBottom);

        if (pCropWidth <= 0 || pCropHeight <= 0)
        {
            PInspectorNoticeShow("The insets remove the whole frame. Reduce them so a crop remains.");
            return;
        }

        if (pInspectorRatioFixed.IsChecked != true)
        {
            if (pInspectorCropPresent)
            {
                PInspectorRatioText(pCropWidth, pCropHeight);
            }

            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        double pRatioWidth = PInspectorNumberRead(pInspectorRatioWidth);
        double pRatioHeight = PInspectorNumberRead(pInspectorRatioHeight);
        if (pRatioWidth <= 0 || pRatioHeight <= 0)
        {
            PInspectorNoticeShow("Enter a ratio to match, for example 16 × 9.");
            return;
        }

        double pWide = pCropWidth * pRatioHeight;
        double pTall = pCropHeight * pRatioWidth;
        double pExcess = pWide > pTall
            ? pCropWidth - (pCropHeight * pRatioWidth / pRatioHeight)
            : pCropHeight - (pCropWidth * pRatioHeight / pRatioWidth);

        int pExcessPixels = PInspectorEvenRead(pExcess);
        if (pExcessPixels <= 0)
        {
            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        PInspectorNoticeShow(pWide > pTall
            ? $"Ratio mismatch — crop {pExcessPixels} px more from left or right."
            : $"Ratio mismatch — crop {pExcessPixels} px more from top or bottom.");
    }

    private void PInspectorRatioText(double pCropWidth, double pCropHeight)
    {
        int pWidthWhole = (int)Math.Round(pCropWidth);
        int pHeightWhole = (int)Math.Round(pCropHeight);
        int pDivisor = PInspectorDivisorRead(pWidthWhole, pHeightWhole);
        pInspectorRatioWidth.Text = (pWidthWhole / pDivisor).ToString(CultureInfo.InvariantCulture);
        pInspectorRatioHeight.Text = (pHeightWhole / pDivisor).ToString(CultureInfo.InvariantCulture);
    }

    private void PInspectorNoticeShow(string pNoticeText)
    {
        pInspectorRatioNotice.Text = pNoticeText;
        pInspectorRatioNotice.Visibility = Visibility.Visible;
    }

    private static string PInspectorEdgeFormat(double pEdgeValue) =>
        Math.Max(0, Math.Round(pEdgeValue)).ToString(CultureInfo.InvariantCulture);

    private static int PInspectorDivisorRead(int pFirst, int pSecond)
    {
        while (pSecond != 0)
        {
            (pFirst, pSecond) = (pSecond, pFirst % pSecond);
        }

        return pFirst == 0 ? 1 : pFirst;
    }

    private static int PInspectorEvenRead(double pExcess)
    {
        if (pExcess < 1)
        {
            return 0;
        }

        int pWhole = (int)Math.Ceiling(pExcess - 0.001);
        return pWhole + (pWhole % 2);
    }

    private static double PInspectorNumberRead(TextBox pNumberBox) =>
        double.TryParse(pNumberBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out double pNumber)
            ? pNumber
            : 0;
}

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed record PSVerdictRow(
    string PSVerdictFamily,
    string PSVerdictEncoder,
    bool PSVerdictSuccess,
    string PSVerdictMessage);

internal sealed class PSVerdict : Window
{
    private const string PSVerdictPlacementKey = "Verdict";

    private const double PSVerdictWidthDefault = 660;
    private const double PSVerdictWidthMinimum = 480;
    private const double PSVerdictHeightDefault = 560;
    private const double PSVerdictHeightMinimum = 360;
    private const double PSVerdictFamilyWidth = 180;
    private const double PSVerdictEncoderWidth = 150;
    private const double PSVerdictStatusWidth = 120;
    private const double PSVerdictInset = 18;
    private const double PSVerdictRowGap = 6;

    private static readonly Brush PSVerdictPassFill = PSVerdictBrushCreate(0xE8, 0xF1, 0xE7);
    private static readonly Brush PSVerdictPassInk = PSVerdictBrushCreate(0x2E, 0x5B, 0x2B);
    private static readonly Brush PSVerdictFailFill = PSVerdictBrushCreate(0xFB, 0xE3, 0xE3);
    private static readonly Brush PSVerdictFailInk = PSVerdictBrushCreate(0x8C, 0x1D, 0x1D);
    private static readonly Brush PSVerdictDetailFill = PSVerdictBrushCreate(0xF1, 0xF5, 0xFA);
    private static readonly FontFamily PSVerdictDetailFont = new("Consolas, Cascadia Mono, monospace");

    private static PSVerdict? psVerdictCurrent;

    private readonly IReadOnlyList<PSVerdictRow> psVerdictRows;
    private readonly string psVerdictTitle;
    private readonly PSGrabber psVerdictGrabber;

    internal static void PSVerdictShow(Window pOwner, string pTitle, IReadOnlyList<PSVerdictRow> pRows)
    {
        psVerdictCurrent?.Close();
        var psVerdict = new PSVerdict(pOwner, pTitle, pRows);
        psVerdictCurrent = psVerdict;
        psVerdict.Show();
    }

    private PSVerdict(Window pOwner, string pTitle, IReadOnlyList<PSVerdictRow> pRows)
    {
        psVerdictRows = pRows;
        psVerdictTitle = pTitle;
        Title = pTitle;
        Owner = pOwner.Owner ?? pOwner;
        ShowInTaskbar = true;
        Width = PSVerdictWidthDefault;
        Height = PSVerdictHeightDefault;
        MinWidth = PSVerdictWidthMinimum;
        MinHeight = PSVerdictHeightMinimum;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSFieldFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        PScrollbar.PScrollbarApply(this);
        Content = PSVerdictBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSVerdictPlacementKey);
        psVerdictGrabber = new PSGrabber(this);
        psVerdictGrabber.PSGrabberAttach();
        Closed += PSVerdictCloseHandle;
    }

    private UIElement PSVerdictBuild()
    {
        var pRoot = new Grid { Background = PSCasement.PSCasementBandFill };
        pRoot.Children.Add(PSVerdictRootBuild());
        pRoot.Children.Add(PSCasement.PSCasementOverlayBuild(this, 0, psVerdictTitle, pCloseOnly: true));
        return pRoot;
    }

    private UIElement PSVerdictRootBuild()
    {
        var pRoot = new DockPanel
        {
            Background = Brushes.White,
            Margin = new Thickness(0, PSCasement.PSCasementBandHeight, 0, 0)
        };

        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
        Button pClose = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Encoder.Verification.Close"));
        pClose.Click += (_, _) => Close();
        pFooter.Children.Add(pClose);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);

        var pBody = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        pBody.Children.Add(PSVerdictTableBuild());

        ScrollViewer pScroll = PSSheet.PSSheetScrollBuild(pBody);
        pScroll.Margin = new Thickness(PSVerdictInset, 0, PSVerdictInset, 8);
        pRoot.Children.Add(pScroll);
        return pRoot;
    }

    private UIElement PSVerdictTableBuild()
    {
        if (psVerdictRows.Count == 0)
        {
            return new TextBlock
            {
                Text = LLocalization.LLocalizationTextRead("Encoder.Verification.NotRun"),
                Foreground = PSFieldMuted,
                Margin = new Thickness(0, 4, 0, 8)
            };
        }

        var pTable = new StackPanel();
        pTable.Children.Add(PSVerdictHeaderBuild());
        foreach (PSVerdictRow pEntry in psVerdictRows)
        {
            pTable.Children.Add(PSVerdictRowBuild(pEntry));
        }

        return PSPlateBuild(pTable);
    }

    private static Grid PSVerdictGridBuild()
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSVerdictFamilyWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSVerdictEncoderWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSVerdictStatusWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return pGrid;
    }

    private static UIElement PSVerdictHeaderBuild()
    {
        Grid pGrid = PSVerdictGridBuild();
        PSVerdictHeaderAdd(pGrid, 0, "Encoder.Verification.Family");
        PSVerdictHeaderAdd(pGrid, 1, "Encoder.Verification.Encoder");
        PSVerdictHeaderAdd(pGrid, 2, "Encoder.Verification.Status");
        pGrid.Margin = new Thickness(0, 0, 0, PSVerdictRowGap);
        return pGrid;
    }

    private static void PSVerdictHeaderAdd(Grid pGrid, int pColumn, string pKey)
    {
        var pHeader = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(pKey),
            Foreground = PSFieldMuted,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(pHeader, pColumn);
        pGrid.Children.Add(pHeader);
    }

    private UIElement PSVerdictRowBuild(PSVerdictRow pEntry)
    {
        Grid pRowGrid = PSVerdictGridBuild();
        pRowGrid.MinHeight = PSFieldChipHeight;
        pRowGrid.Margin = new Thickness(0, 0, 0, PSVerdictRowGap);
        PSVerdictCellAdd(pRowGrid, 0, PSVerdictTextBuild(pEntry.PSVerdictFamily, PSFieldText));
        PSVerdictCellAdd(pRowGrid, 1, PSVerdictTextBuild(pEntry.PSVerdictEncoder, PSFieldMuted));
        PSVerdictCellAdd(pRowGrid, 2, PSVerdictBadgeBuild(pEntry.PSVerdictSuccess));

        var pRow = new StackPanel();
        pRow.Children.Add(pRowGrid);
        if (!PSVerdictDetailCheck(pEntry))
        {
            return pRow;
        }

        var pDetail = new Border
        {
            Background = PSVerdictDetailFill,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, PSVerdictRowGap),
            Visibility = Visibility.Collapsed,
            Child = new TextBlock
            {
                Text = pEntry.PSVerdictMessage,
                Foreground = PSFieldMuted,
                FontFamily = PSVerdictDetailFont,
                TextWrapping = TextWrapping.Wrap
            }
        };

        Button pChip = PSVerdictChipBuild(false);
        pChip.HorizontalAlignment = HorizontalAlignment.Left;
        pChip.Click += (_, _) =>
        {
            bool pShown = pDetail.Visibility == Visibility.Visible;
            pDetail.Visibility = pShown ? Visibility.Collapsed : Visibility.Visible;
            pChip.Content = PSVerdictChipText(!pShown);
        };

        PSVerdictCellAdd(pRowGrid, 3, pChip);
        pRow.Children.Add(pDetail);
        return pRow;
    }

    private static bool PSVerdictDetailCheck(PSVerdictRow pEntry)
    {
        string pMessage = pEntry.PSVerdictMessage.Trim();
        if (pMessage.Length == 0)
        {
            return false;
        }

        return !(pEntry.PSVerdictSuccess && string.Equals(pMessage, "exit 0", StringComparison.OrdinalIgnoreCase));
    }

    private static void PSVerdictCellAdd(Grid pGrid, int pColumn, UIElement pCell)
    {
        Grid.SetColumn(pCell, pColumn);
        pGrid.Children.Add(pCell);
    }

    private static TextBlock PSVerdictTextBuild(string pText, Brush pInk) => new()
    {
        Text = pText,
        Foreground = pInk,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0)
    };

    private static UIElement PSVerdictBadgeBuild(bool pSuccess) => new Border
    {
        Background = pSuccess ? PSVerdictPassFill : PSVerdictFailFill,
        CornerRadius = new CornerRadius(3),
        Padding = new Thickness(8, 1, 8, 1),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Child = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(pSuccess
                ? "Encoder.Verification.Available"
                : "Encoder.Verification.Unavailable"),
            Foreground = pSuccess ? PSVerdictPassInk : PSVerdictFailInk,
            FontWeight = FontWeights.SemiBold
        }
    };

    private static Button PSVerdictChipBuild(bool pShown) => new()
    {
        Content = PSVerdictChipText(pShown),
        Height = PSFieldChipHeight,
        Padding = new Thickness(10, 0, 10, 0),
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 11,
        Style = PButton.PButtonWhiteCreate()
    };

    private static string PSVerdictChipText(bool pShown) =>
        LLocalization.LLocalizationTextRead("Encoder.Verification.Details") + (pShown ? "  ▴" : "  ▾");

    private static Brush PSVerdictBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    private void PSVerdictCloseHandle(object? pSender, EventArgs pEvent)
    {
        PSGrabber.PSGrabberPlacementSave(this, PSVerdictPlacementKey);
        psVerdictGrabber.PSGrabberDetach();
        Closed -= PSVerdictCloseHandle;
        if (ReferenceEquals(psVerdictCurrent, this))
        {
            psVerdictCurrent = null;
        }
    }

    protected override void OnSourceInitialized(EventArgs pEvent)
    {
        base.OnSourceInitialized(pEvent);
        PSCasement.PSCasementDwmApply(this);
    }
}

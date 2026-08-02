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
    private const double PSVerdictFamilyWidth = 190;
    private const double PSVerdictEncoderWidth = 150;
    private const double PSVerdictResultWidth = 70;
    private const double PSVerdictRowGap = 6;

    private static readonly Brush PSVerdictPassFill = PSVerdictBrushCreate(0xE8, 0xF1, 0xE7);
    private static readonly Brush PSVerdictPassInk = PSVerdictBrushCreate(0x2E, 0x5B, 0x2B);
    private static readonly Brush PSVerdictFailFill = PSVerdictBrushCreate(0xFB, 0xE3, 0xE3);
    private static readonly Brush PSVerdictFailInk = PSVerdictBrushCreate(0x8C, 0x1D, 0x1D);

    private readonly IReadOnlyList<PSVerdictRow> psVerdictRows;
    private readonly PSGrabber psVerdictGrabber;

    internal static void PSVerdictShow(Window pOwner, string pTitle, IReadOnlyList<PSVerdictRow> pRows)
    {
        var psVerdict = new PSVerdict(pOwner, pTitle, pRows);
        psVerdict.ShowDialog();
    }

    private PSVerdict(Window pOwner, string pTitle, IReadOnlyList<PSVerdictRow> pRows)
    {
        psVerdictRows = pRows;
        Title = pTitle;
        Owner = pOwner;
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
        pRoot.Children.Add(PSCasement.PSCasementOverlayBuild(this, 0, null, pCloseOnly: true));
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

        var pBody = new StackPanel { Margin = new Thickness(18, 14, 18, 0) };
        pBody.Children.Add(PSVerdictTableBuild());
        pRoot.Children.Add(PSSheet.PSSheetScrollBuild(pBody));
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

        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSVerdictFamilyWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSVerdictEncoderWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSVerdictResultWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        PSVerdictHeaderAdd(pGrid, 0, "Encoder.Verification.Family");
        PSVerdictHeaderAdd(pGrid, 1, "Encoder.Verification.Encoder");
        PSVerdictHeaderAdd(pGrid, 2, "Encoder.Verification.Result");
        PSVerdictHeaderAdd(pGrid, 3, "Encoder.Verification.Detail");

        int pRow = 1;
        foreach (PSVerdictRow pEntry in psVerdictRows)
        {
            pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            PSVerdictCellAdd(pGrid, pRow, 0, PSVerdictTextBuild(pEntry.PSVerdictFamily, PSFieldText));
            PSVerdictCellAdd(pGrid, pRow, 1, PSVerdictTextBuild(pEntry.PSVerdictEncoder, PSFieldMuted));
            PSVerdictCellAdd(pGrid, pRow, 2, PSVerdictBadgeBuild(pEntry.PSVerdictSuccess));
            PSVerdictCellAdd(pGrid, pRow, 3, PSVerdictTextBuild(pEntry.PSVerdictMessage, PSFieldMuted));
            pRow++;
        }

        return PSPlateBuild(pGrid);
    }

    private static void PSVerdictHeaderAdd(Grid pGrid, int pColumn, string pKey)
    {
        var pHeader = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(pKey),
            Foreground = PSFieldMuted,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 10, PSVerdictRowGap)
        };
        Grid.SetRow(pHeader, 0);
        Grid.SetColumn(pHeader, pColumn);
        pGrid.Children.Add(pHeader);
    }

    private static void PSVerdictCellAdd(Grid pGrid, int pRow, int pColumn, UIElement pCell)
    {
        Grid.SetRow(pCell, pRow);
        Grid.SetColumn(pCell, pColumn);
        pGrid.Children.Add(pCell);
    }

    private static TextBlock PSVerdictTextBuild(string pText, Brush pInk) => new()
    {
        Text = pText,
        Foreground = pInk,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, PSVerdictRowGap)
    };

    private static UIElement PSVerdictBadgeBuild(bool pSuccess)
    {
        var pBadge = new Border
        {
            Background = pSuccess ? PSVerdictPassFill : PSVerdictFailFill,
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8, 1, 8, 1),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, PSVerdictRowGap),
            Child = new TextBlock
            {
                Text = LLocalization.LLocalizationTextRead(pSuccess ? "Encoder.Verification.Pass" : "Encoder.Verification.Fail"),
                Foreground = pSuccess ? PSVerdictPassInk : PSVerdictFailInk,
                FontWeight = FontWeights.SemiBold
            }
        };
        return pBadge;
    }

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
    }

    protected override void OnSourceInitialized(EventArgs pEvent)
    {
        base.OnSourceInitialized(pEvent);
        PSCasement.PSCasementDwmApply(this);
    }
}

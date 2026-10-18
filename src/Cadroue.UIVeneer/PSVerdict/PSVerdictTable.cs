using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSVerdictTable
{
    private const double PSVerdictRowGap = 6;

    private const double PSVerdictFamilyWidth = 180;
    private const double PSVerdictEncoderWidth = 150;
    private const double PSVerdictStatusWidth = 120;

    private static readonly Brush PSVerdictPassFill = PSVerdictDetail.PSVerdictBrushCreate(0xE8, 0xF1, 0xE7);
    private static readonly Brush PSVerdictPassInk = PSVerdictDetail.PSVerdictBrushCreate(0x2E, 0x5B, 0x2B);
    private static readonly Brush PSVerdictFailFill = PSVerdictDetail.PSVerdictBrushCreate(0xFB, 0xE3, 0xE3);
    private static readonly Brush PSVerdictFailInk = PSVerdictDetail.PSVerdictBrushCreate(0x8C, 0x1D, 0x1D);

    internal static UIElement PSVerdictTableBuild(IReadOnlyList<QSVerdictRow> pRows)
    {
        if (pRows.Count == 0)
        {
            return new TextBlock
            {
                Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.NotRun"),
                Foreground = PSField.PSFieldMuted,
                Margin = new Thickness(0, 4, 0, 8)
            };
        }

        var pTable = new StackPanel();
        pTable.Children.Add(PSVerdictHeaderBuild());
        foreach (QSVerdictRow pEntry in pRows)
        {
            pTable.Children.Add(PSVerdictRowBuild(pEntry));
        }

        return PSPlate.PSPlateBuild(pTable);
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
            Text = QLocalizationCatalog.QLocalizationTextRead(pKey),
            Foreground = PSField.PSFieldMuted,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(pHeader, pColumn);
        pGrid.Children.Add(pHeader);
    }

    private static UIElement PSVerdictRowBuild(QSVerdictRow pEntry)
    {
        (StackPanel pRow, Grid pRowGrid) = PSVerdictRowCreate(pEntry);
        if (!QSVerdict.QSVerdictDetailCheck(pEntry))
        {
            return pRow;
        }

        Border pDetail = PSVerdictDetail.PSVerdictDetailBuild(pEntry);
        Button pChip = PSVerdictDetail.PSVerdictChipBuild(false);
        PSVerdictDetail.PSVerdictChipAttach(pChip, pDetail);
        PSVerdictDetailAdd(pRow, pRowGrid, pChip, pDetail);
        return pRow;
    }

    private static (StackPanel PSVerdictRow, Grid PSVerdictGrid) PSVerdictRowCreate(QSVerdictRow pEntry)
    {
        Grid pRowGrid = PSVerdictGridBuild();
        pRowGrid.MinHeight = PSField.PSFieldChipHeight;
        pRowGrid.Margin = new Thickness(0, 0, 0, PSVerdictRowGap);
        PSVerdictCellAdd(pRowGrid, 0, PSVerdictTextBuild(pEntry.QSVerdictFamily, PSField.PSFieldText));
        PSVerdictCellAdd(pRowGrid, 1, PSVerdictTextBuild(pEntry.QSVerdictEncoder, PSField.PSFieldMuted));
        PSVerdictCellAdd(pRowGrid, 2, PSVerdictBadgeBuild(pEntry.QSVerdictSuccess));

        var pRow = new StackPanel();
        pRow.Children.Add(pRowGrid);
        return (pRow, pRowGrid);
    }

    private static void PSVerdictDetailAdd(StackPanel pRow, Grid pRowGrid, Button pChip, Border pDetail)
    {
        pChip.HorizontalAlignment = HorizontalAlignment.Left;
        PSVerdictCellAdd(pRowGrid, 3, pChip);
        pDetail.Margin = new Thickness(0, 0, 0, PSVerdictRowGap);
        pRow.Children.Add(pDetail);
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

    private static UIElement PSVerdictBadgeBuild(bool pSuccess)
    {
        (Brush pFill, Brush pInk) = PSVerdictTintRead(pSuccess);
        string pLabel = PSVerdictBadgeFormat(pSuccess);
        return PSVerdictBadgeCreate(pFill, pInk, pLabel);
    }

    private static (Brush PSVerdictFill, Brush PSVerdictInk) PSVerdictTintRead(bool pSuccess) =>
        (pSuccess ? PSVerdictPassFill : PSVerdictFailFill, pSuccess ? PSVerdictPassInk : PSVerdictFailInk);

    private static string PSVerdictBadgeFormat(bool pSuccess) =>
        QLocalizationCatalog.QLocalizationTextRead(pSuccess
            ? "Encoder.Verification.Available"
            : "Encoder.Verification.Unavailable");

    private static Border PSVerdictBadgeCreate(Brush pFill, Brush pInk, string pLabel) => new()
    {
        Background = pFill,
        CornerRadius = new CornerRadius(3),
        Padding = new Thickness(8, 1, 8, 1),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Child = new TextBlock
        {
            Text = pLabel,
            Foreground = pInk,
            FontWeight = FontWeights.SemiBold
        }
    };
}

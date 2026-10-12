using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterCard
{
    private static readonly IReadOnlyDictionary<string, Func<QRoster, QRosterEntry, ContextMenu, UIElement>>
        PRosterEntryBuilders = new Dictionary<string, Func<QRoster, QRosterEntry, ContextMenu, UIElement>>
        {
            [QRosterEntry.QRosterKindHead] = (lRoster, lEntry, _) =>
                PRosterHeaderBuild(lRoster, lEntry.QRosterEntryCard),
            [QRosterEntry.QRosterKindFile] = (_, lEntry, _) => PRosterFileBuild(lEntry.QRosterEntryFile!),
            [QRosterEntry.QRosterKindRow] = (lRoster, lEntry, pMenu) =>
                PRosterRow.PRosterRowBuild(lRoster, lEntry.QRosterEntryRow!, pMenu)
        };

    public static Border PRosterEntryBuild(QRoster lRoster, QRosterEntry lEntry, ContextMenu pMenu) => new()
    {
        Background = PRosterTheme.PRosterBodyFills[lEntry.QRosterEntryCard.QRosterCardBody],
        BorderBrush = PRosterTheme.PRosterBodyLines[lEntry.QRosterEntryCard.QRosterCardBody],
        BorderThickness = PRosterTheme.PRosterFrameBorders[lEntry.QRosterEntryFrame],
        CornerRadius = PRosterTheme.PRosterFrameCorners[lEntry.QRosterEntryFrame],
        Padding = PRosterTheme.PRosterFramePaddings[lEntry.QRosterEntryFrame],
        Margin = PRosterTheme.PRosterFrameMargins[lEntry.QRosterEntryFrame],
        Child = PRosterEntryBuilders[lEntry.QRosterEntryKind](lRoster, lEntry, pMenu)
    };

    private static Border PRosterFileBuild(QRosterFile lFile) => new()
    {
        Background = PRosterTheme.PRosterRowShades[lFile.QRosterFileShade],
        Padding = new Thickness(12, 5, 12, 3),
        Child = new TextBlock
        {
            Text = lFile.QRosterFileTitle,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterTitleBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        }
    };

    private static Border PRosterHeaderBuild(QRoster lRoster, QRosterCard lCard)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pTitleCell = new TextBlock
        {
            Text = lCard.QRosterCardTitle,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterTitleBrushes[lCard.QRosterCardKey],
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Cursor = Cursors.Hand
        };
        pTitleCell.MouseLeftButtonUp += (_, _) => lRoster.QRosterSelection.QRosterCardSelect(lCard.QRosterCardBatch);
        Grid.SetColumn(pTitleCell, 0);
        pGrid.Children.Add(pTitleCell);

        var pControls = new StackPanel { Orientation = Orientation.Horizontal };
        pControls.Children.Add(PRosterToggleBuild(lRoster, lCard));
        pControls.Children.Add(PRosterCloseBuild(lRoster, lCard));
        Grid.SetColumn(pControls, 1);
        pGrid.Children.Add(pControls);

        var pHeader = new Border
        {
            Padding = new Thickness(12, 6, 6, 6),
            Background = PRosterTheme.PRosterCardFills[lCard.QRosterCardKey],
            BorderBrush = PRosterTheme.PRosterCardLines[lCard.QRosterCardKey],
            BorderThickness = PRosterTheme.PRosterHeaderBorders[lCard.QRosterCardCollapsed],
            CornerRadius = PRosterTheme.PRosterHeaderCorners[lCard.QRosterCardCollapsed],
            Child = pGrid
        };
        pHeader.MouseEnter += (_, _) => pHeader.Background = PRosterTheme.PRosterCardHovers[lCard.QRosterCardKey];
        pHeader.MouseLeave += (_, _) => pHeader.Background = PRosterTheme.PRosterCardFills[lCard.QRosterCardKey];
        return pHeader;
    }

    private static Border PRosterToggleBuild(QRoster lRoster, QRosterCard lCard)
    {
        string pIconPath = PRosterTheme.PRosterToggleIcons[lCard.QRosterCardCollapsed];
        Brush pControlBrush = PRosterTheme.PRosterControlBrushes[lCard.QRosterCardKey];
        var pIcon = new Image
        {
            Source = PIcon.PIconRead(pIconPath, pControlBrush),
            Width = 20,
            Height = 20,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Border pButton = PRosterControlBuild(pIcon, lCard.QRosterCardTooltip);
        pButton.Margin = new Thickness(0, 0, 2, 0);
        pButton.MouseEnter += (_, _) =>
        {
            pButton.Background = PRosterTheme.PRosterControlHovers[lCard.QRosterCardKey];
            pIcon.Source = PIcon.PIconRead(pIconPath, PRosterTheme.PRosterTextBrush);
        };
        pButton.MouseLeave += (_, _) =>
        {
            pButton.Background = Brushes.Transparent;
            pIcon.Source = PIcon.PIconRead(pIconPath, pControlBrush);
        };
        pButton.MouseLeftButtonUp += (_, _) => lRoster.QRosterCollapse.QRosterCollapseToggle(lCard.QRosterCardBatch);
        return pButton;
    }

    private static Border PRosterCloseBuild(QRoster lRoster, QRosterCard lCard)
    {
        Brush pControlBrush = PRosterTheme.PRosterControlBrushes[lCard.QRosterCardKey];
        var pGlyph = new TextBlock
        {
            Text = "✕",
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = pControlBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Border pButton = PRosterControlBuild(pGlyph, LLocalization.LLocalizationTextRead("Roster.Card.Remove"));
        pButton.MouseEnter += (_, _) =>
        {
            pButton.Background = PRosterTheme.PRosterControlHovers[lCard.QRosterCardKey];
            pGlyph.Foreground = PRosterTheme.PRosterFailBrush;
        };
        pButton.MouseLeave += (_, _) =>
        {
            pButton.Background = Brushes.Transparent;
            pGlyph.Foreground = pControlBrush;
        };
        pButton.MouseLeftButtonUp += (_, _) => lRoster.QRosterRemove(lCard.QRosterCardBatch);
        return pButton;
    }

    private static Border PRosterControlBuild(UIElement pContent, string pTooltip)
    {
        var pButton = new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = pTooltip,
            Child = pContent
        };
        pButton.MouseLeftButtonDown += (_, pArgs) => pArgs.Handled = true;
        return pButton;
    }
}

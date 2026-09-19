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
    public static Border PRosterCardBuild(LRoster lRoster, LRosterCard lCard)
    {
        var pDetail = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 6),
            Visibility = PLook.PLookVisible[!lCard.LRosterCardCollapsed]
        };
        lCard.LRosterCardFiles.ToList().ForEach(lFile => PRosterFileAdd(pDetail, lRoster, lFile));

        var pStack = new StackPanel();
        pStack.Children.Add(PRosterHeaderBuild(lRoster, lCard));
        pStack.Children.Add(pDetail);

        return new Border
        {
            CornerRadius = new CornerRadius(PRosterTheme.PRosterCorner),
            Background = PRosterTheme.PRosterBodyFills[lCard.LRosterCardBody],
            BorderBrush = PRosterTheme.PRosterBodyLines[lCard.LRosterCardBody],
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 8),
            Child = pStack
        };
    }

    private static void PRosterFileAdd(StackPanel pDetail, LRoster lRoster, LRosterFile lFile)
    {
        pDetail.Children.Add(new Border
        {
            Background = PRosterTheme.PRosterRowShades[lFile.LRosterFileShade],
            Padding = new Thickness(12, 5, 12, 3),
            Child = new TextBlock
            {
                Text = lFile.LRosterFileTitle,
                FontSize = PRosterTheme.PRosterRowSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = PRosterTheme.PRosterTitleBrush,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        });
        lFile.LRosterFileRows.ToList().ForEach(lRow => pDetail.Children.Add(PRosterRow.PRosterRowBuild(lRoster, lRow)));
    }

    private static Border PRosterHeaderBuild(LRoster lRoster, LRosterCard lCard)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pTitleCell = new TextBlock
        {
            Text = lCard.LRosterCardTitle,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterTitleBrushes[lCard.LRosterCardKey],
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Cursor = Cursors.Hand
        };
        pTitleCell.MouseLeftButtonUp += (_, _) => lRoster.LRosterCardSelect(lCard.LRosterCardBatch);
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
            Background = PRosterTheme.PRosterCardFills[lCard.LRosterCardKey],
            BorderBrush = PRosterTheme.PRosterCardLines[lCard.LRosterCardKey],
            BorderThickness = PRosterTheme.PRosterHeaderBorders[lCard.LRosterCardCollapsed],
            CornerRadius = PRosterTheme.PRosterHeaderCorners[lCard.LRosterCardCollapsed],
            Child = pGrid
        };
        pHeader.MouseEnter += (_, _) => pHeader.Background = PRosterTheme.PRosterCardHovers[lCard.LRosterCardKey];
        pHeader.MouseLeave += (_, _) => pHeader.Background = PRosterTheme.PRosterCardFills[lCard.LRosterCardKey];
        return pHeader;
    }

    private static Border PRosterToggleBuild(LRoster lRoster, LRosterCard lCard)
    {
        string pIconPath = PRosterTheme.PRosterToggleIcons[lCard.LRosterCardCollapsed];
        Brush pControlBrush = PRosterTheme.PRosterControlBrushes[lCard.LRosterCardKey];
        var pIcon = new Image
        {
            Source = PIcon.PIconRead(pIconPath, pControlBrush),
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Border pButton = PRosterControlBuild(pIcon, lCard.LRosterCardTooltip);
        pButton.Margin = new Thickness(0, 0, 2, 0);
        pButton.MouseEnter += (_, _) =>
        {
            pButton.Background = PRosterTheme.PRosterControlHovers[lCard.LRosterCardKey];
            pIcon.Source = PIcon.PIconRead(pIconPath, PRosterTheme.PRosterTextBrush);
        };
        pButton.MouseLeave += (_, _) =>
        {
            pButton.Background = Brushes.Transparent;
            pIcon.Source = PIcon.PIconRead(pIconPath, pControlBrush);
        };
        pButton.MouseLeftButtonUp += (_, _) => lRoster.LRosterCollapseToggle(lCard.LRosterCardBatch);
        return pButton;
    }

    private static Border PRosterCloseBuild(LRoster lRoster, LRosterCard lCard)
    {
        Brush pControlBrush = PRosterTheme.PRosterControlBrushes[lCard.LRosterCardKey];
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
            pButton.Background = PRosterTheme.PRosterControlHovers[lCard.LRosterCardKey];
            pGlyph.Foreground = PRosterTheme.PRosterFailBrush;
        };
        pButton.MouseLeave += (_, _) =>
        {
            pButton.Background = Brushes.Transparent;
            pGlyph.Foreground = pControlBrush;
        };
        pButton.MouseLeftButtonUp += (_, _) => lRoster.LRosterRemove(lCard.LRosterCardBatch);
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

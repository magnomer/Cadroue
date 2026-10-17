using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
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
        TextBlock pTitleCell = PRosterCaptionBuild(lCard);
        PRosterTitleAttach(pTitleCell, lRoster, lCard);
        Border pHeader = PRosterBannerBuild(lRoster, lCard, pTitleCell);
        PRosterBannerAttach(pHeader, lCard);
        return pHeader;
    }

    private static TextBlock PRosterCaptionBuild(QRosterCard lCard) => new()
    {
        Text = lCard.QRosterCardTitle,
        FontSize = PRosterTheme.PRosterRowSize,
        FontWeight = FontWeights.SemiBold,
        Foreground = PRosterTheme.PRosterTitleBrushes[lCard.QRosterCardKey],
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
        Cursor = Cursors.Hand
    };

    private static void PRosterTitleAttach(TextBlock pTitleCell, QRoster lRoster, QRosterCard lCard) =>
        pTitleCell.MouseLeftButtonUp += (_, _) => lRoster.QRosterSelection.QRosterCardSelect(lCard.QRosterCardBatch);

    private static void PRosterBannerAttach(Border pHeader, QRosterCard lCard)
    {
        pHeader.MouseEnter += (_, _) => PRosterFillSet(pHeader, PRosterTheme.PRosterCardHovers[lCard.QRosterCardKey]);
        pHeader.MouseLeave += (_, _) => PRosterFillSet(pHeader, PRosterTheme.PRosterCardFills[lCard.QRosterCardKey]);
    }

    private static void PRosterFillSet(Border pHeader, Brush pFill) => pHeader.Background = pFill;

    private static Border PRosterBannerBuild(QRoster lRoster, QRosterCard lCard, TextBlock pTitleCell)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(pTitleCell, 0);
        pGrid.Children.Add(pTitleCell);

        var pControls = new StackPanel { Orientation = Orientation.Horizontal };
        pControls.Children.Add(PRosterToggleBuild(lRoster, lCard));
        pControls.Children.Add(PRosterCloseBuild(lRoster, lCard));
        Grid.SetColumn(pControls, 1);
        pGrid.Children.Add(pControls);

        return new Border
        {
            Padding = new Thickness(12, 6, 6, 6),
            Background = PRosterTheme.PRosterCardFills[lCard.QRosterCardKey],
            BorderBrush = PRosterTheme.PRosterCardLines[lCard.QRosterCardKey],
            BorderThickness = PRosterTheme.PRosterHeaderBorders[lCard.QRosterCardCollapsed],
            CornerRadius = PRosterTheme.PRosterHeaderCorners[lCard.QRosterCardCollapsed],
            Child = pGrid
        };
    }

    private static Border PRosterToggleBuild(QRoster lRoster, QRosterCard lCard)
    {
        string pIconPath = PRosterTheme.PRosterToggleIcons[lCard.QRosterCardCollapsed];
        Brush pControlBrush = PRosterTheme.PRosterControlBrushes[lCard.QRosterCardKey];
        Image pIcon = PRosterIconBuild(pIconPath, pControlBrush);
        Border pButton = PRosterControlBuild(pIcon, lCard.QRosterCardTooltip);
        PRosterMarginSet(pButton);
        PRosterToggleAttach(lRoster, lCard, pButton, pIcon, pIconPath, pControlBrush);
        return pButton;
    }

    private static Image PRosterIconBuild(string pIconPath, Brush pControlBrush) => new()
    {
        Source = QIcon.QIconRead(pIconPath, pControlBrush),
        Width = 20,
        Height = 20,
        Stretch = Stretch.Uniform,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static void PRosterMarginSet(Border pButton) => pButton.Margin = new Thickness(0, 0, 2, 0);

    private static void PRosterToggleAttach(
        QRoster lRoster,
        QRosterCard lCard,
        Border pButton,
        Image pIcon,
        string pIconPath,
        Brush pControlBrush)
    {
        pButton.MouseEnter += (_, _) => PRosterIconApply(
            pButton,
            PRosterTheme.PRosterControlHovers[lCard.QRosterCardKey],
            pIcon,
            pIconPath,
            PRosterTheme.PRosterTextBrush);
        pButton.MouseLeave += (_, _) =>
            PRosterIconApply(pButton, Brushes.Transparent, pIcon, pIconPath, pControlBrush);
        pButton.MouseLeftButtonUp += (_, _) => lRoster.QRosterCollapse.QRosterCollapseToggle(lCard.QRosterCardBatch);
    }

    private static void PRosterIconApply(
        Border pButton,
        Brush pFill,
        Image pIcon,
        string pIconPath,
        Brush pIconBrush)
    {
        pButton.Background = pFill;
        pIcon.Source = QIcon.QIconRead(pIconPath, pIconBrush);
    }

    private static Border PRosterCloseBuild(QRoster lRoster, QRosterCard lCard)
    {
        Brush pControlBrush = PRosterTheme.PRosterControlBrushes[lCard.QRosterCardKey];
        TextBlock pGlyph = PRosterGlyphBuild(pControlBrush);
        Border pButton = PRosterControlBuild(pGlyph, QLocalizationCatalog.QLocalizationTextRead("Roster.Card.Remove"));
        PRosterCloseAttach(lRoster, lCard, pButton, pGlyph, pControlBrush);
        return pButton;
    }

    private static TextBlock PRosterGlyphBuild(Brush pControlBrush) => new()
    {
        Text = "✕",
        FontSize = PRosterTheme.PRosterRowSize,
        Foreground = pControlBrush,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static void PRosterCloseAttach(
        QRoster lRoster,
        QRosterCard lCard,
        Border pButton,
        TextBlock pGlyph,
        Brush pControlBrush)
    {
        pButton.MouseEnter += (_, _) => PRosterGlyphApply(
            pButton,
            PRosterTheme.PRosterControlHovers[lCard.QRosterCardKey],
            pGlyph,
            PRosterTheme.PRosterFailBrush);
        pButton.MouseLeave += (_, _) => PRosterGlyphApply(pButton, Brushes.Transparent, pGlyph, pControlBrush);
        pButton.MouseLeftButtonUp += (_, _) => lRoster.QRosterRemove(lCard.QRosterCardBatch);
    }

    private static void PRosterGlyphApply(Border pButton, Brush pFill, TextBlock pGlyph, Brush pGlyphBrush)
    {
        pButton.Background = pFill;
        pGlyph.Foreground = pGlyphBrush;
    }

    private static Border PRosterControlBuild(UIElement pContent, string pTooltip)
    {
        Border pButton = PRosterButtonBuild(pContent, pTooltip);
        PRosterPressAttach(pButton);
        return pButton;
    }

    private static Border PRosterButtonBuild(UIElement pContent, string pTooltip) => new()
    {
        Width = 20,
        Height = 20,
        CornerRadius = new CornerRadius(4),
        Background = Brushes.Transparent,
        Cursor = Cursors.Hand,
        ToolTip = pTooltip,
        Child = pContent
    };

    private static void PRosterPressAttach(Border pButton) => pButton.MouseLeftButtonDown += PRosterPressHandle;

    private static void PRosterPressHandle(object pSender, MouseButtonEventArgs pArgs) => pArgs.Handled = true;
}

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PRoster
{
    private UIElement PRosterCloseBuild(IReadOnlyList<LWorkItem> pBatchItems)
    {
        Guid pBatchId = pBatchItems[0].LWorkBatchId;
        var pGlyph = new TextBlock
        {
            Text = "✕",
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = PRosterControlRead(pBatchId),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pButton = new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead("Roster.Card.Remove"),
            Child = pGlyph
        };

        pButton.MouseEnter += (_, _) =>
        {
            pButton.Background = PRosterHoverRead(pBatchId);
            pGlyph.Foreground = PRosterTheme.PRosterFailBrush;
        };
        pButton.MouseLeave += (_, _) =>
        {
            pButton.Background = Brushes.Transparent;
            pGlyph.Foreground = PRosterControlRead(pBatchId);
        };
        pButton.MouseLeftButtonDown += (_, pArgs) => pArgs.Handled = true;
        pButton.MouseLeftButtonUp += (_, _) => PRosterCardRemove(pBatchItems);
        pRosterCloseGlyphs[pBatchId] = pGlyph;

        return pButton;
    }

    private const string pRosterMaximizeIcon = "/PAsset/PPanel/PRosterBatchMaximize.svg";
    private const string pRosterMinimizeIcon = "/PAsset/PPanel/PRosterBatchMinimize.svg";

    private static ImageSource PRosterMinimizeRead(bool pCollapsed, Brush pTint) =>
        PIcon.PIconRead(pCollapsed ? pRosterMaximizeIcon : pRosterMinimizeIcon, pTint);

    private UIElement PRosterMinimizeBuild(Guid pBatchId, StackPanel pDetail)
    {
        bool pCollapsed = LRoster.LRosterCollapsedCheck(pBatchId);
        var pIcon = new Image
        {
            Source = PRosterMinimizeRead(pCollapsed, PRosterControlRead(pBatchId)),
            Width = 14,
            Height = 14,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pButton = new Border
        {
            Width = 20,
            Height = 20,
            Margin = new Thickness(0, 0, 2, 0),
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead(pCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse"),
            Child = pIcon
        };

        pButton.MouseEnter += (_, _) =>
        {
            pButton.Background = PRosterHoverRead(pBatchId);
            pIcon.Source = PRosterMinimizeRead(
                LRoster.LRosterCollapsedCheck(pBatchId), PRosterTheme.PRosterTextBrush);
        };
        pButton.MouseLeave += (_, _) =>
        {
            pButton.Background = Brushes.Transparent;
            pIcon.Source = PRosterMinimizeRead(
                LRoster.LRosterCollapsedCheck(pBatchId), PRosterControlRead(pBatchId));
        };
        pButton.MouseLeftButtonDown += (_, pArgs) => pArgs.Handled = true;
        pButton.MouseLeftButtonUp += (_, _) => PRosterMinimizeToggle(pBatchId);
        pRosterBatchControls[pBatchId] = new PRosterBatchControl(pDetail, pButton, pIcon);

        return pButton;
    }

    private void PRosterMinimizeToggle(Guid pBatchId)
    {
        PRosterBatchApply(pBatchId, LRoster.LRosterCollapseToggle(pBatchId));
    }

    private void PRosterBatchApply(Guid pBatchId, bool pCollapsed)
    {
        if (pRosterCardHeaders.TryGetValue(pBatchId, out Border? pHeader))
        {
            PRosterCollapseApply(pHeader, pCollapsed);
            PRosterVisualApply(pBatchId, pHeader);
        }

        if (pRosterBatchControls.TryGetValue(pBatchId, out PRosterBatchControl? pControl))
        {
            pControl.PRosterBatchDetail.Visibility = pCollapsed ? Visibility.Collapsed : Visibility.Visible;
            pControl.PRosterBatchIcon.Source = PRosterMinimizeRead(
                pCollapsed, PRosterControlRead(pBatchId));
            pControl.PRosterBatchButton.ToolTip = LLocalization.LLocalizationTextRead(
                pCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse");
        }
    }
}

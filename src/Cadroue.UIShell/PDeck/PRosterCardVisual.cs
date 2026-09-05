using System.Globalization;
using System.Windows;
using Cadroue.UIShell.PSCasement;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIShell.PAsset;

namespace Cadroue.UIShell.PDeck;

public sealed partial class PRoster
{
    private static void PRosterCollapseApply(Border pHeader, bool pCollapsed)
    {
        pHeader.BorderThickness = pCollapsed ? new Thickness(0) : new Thickness(0, 0, 0, 1);
        pHeader.CornerRadius = pCollapsed
            ? new CornerRadius(PRosterTheme.PRosterCorner)
            : new CornerRadius(PRosterTheme.PRosterCorner, PRosterTheme.PRosterCorner, 0, 0);
    }

    private void PRosterCardApply()
    {
        foreach ((Guid pBatchId, Border pHeader) in pRosterCardHeaders)
        {
            PRosterVisualApply(pBatchId, pHeader);
        }
    }

    private void PRosterVisualApply(Guid pBatchId, Border pHeader)
    {
        bool pSelected = pBatchId == pRosterCardId;
        bool pCompleted = pRosterCompletedIds.Contains(pBatchId);
        pHeader.Background = pSelected
            ? PRosterTheme.PRosterSelectCard
            : pCompleted
                ? PRosterTheme.PRosterDoneCard
                : PRosterTheme.PRosterCardBrush;
        pHeader.BorderBrush = pSelected
            ? PRosterTheme.PRosterSelectLine
            : pCompleted
                ? PRosterTheme.PRosterDoneLine
                : PRosterTheme.PRosterCardLine;

        if (pRosterCards.TryGetValue(pBatchId, out Border? pCard))
        {
            pCard.Background = pCompleted
                ? PRosterTheme.PRosterDoneBody
                : pSelected
                    ? PRosterTheme.PRosterSelectBody
                    : PRosterTheme.PRosterBodyBrush;
            pCard.BorderBrush = pCompleted
                ? PRosterTheme.PRosterDoneLine
                : pSelected
                    ? PRosterTheme.PRosterOuterLine
                    : PRosterTheme.PRosterCardLine;
        }

        if (pRosterCardTitles.TryGetValue(pBatchId, out TextBlock? pTitle))
        {
            pTitle.Foreground = pSelected
                ? PRosterTheme.PRosterSelectText
                : pCompleted
                    ? PRosterTheme.PRosterMutedBrush
                    : PRosterTheme.PRosterTitleBrush;
        }

        Brush pControlBrush = pSelected
            ? PRosterTheme.PRosterSelectText
            : PRosterTheme.PRosterMutedBrush;
        if (pRosterBatchControls.TryGetValue(pBatchId, out PRosterBatchControl? pControl))
        {
            pControl.PRosterBatchIcon.Source = PRosterMinimizeRead(
                pRosterCollapsedIds.Contains(pBatchId), pControlBrush);
        }

        if (pRosterCloseGlyphs.TryGetValue(pBatchId, out TextBlock? pCloseGlyph))
        {
            pCloseGlyph.Foreground = pControlBrush;
        }
    }

    private Brush PRosterHoverRead(Guid pBatchId) =>
        pBatchId == pRosterCardId
            ? PRosterTheme.PRosterCardBrush
            : pRosterCompletedIds.Contains(pBatchId)
                ? PRosterTheme.PRosterDoneHover
                : PRosterTheme.PRosterControlHover;

    private Brush PRosterControlRead(Guid pBatchId) =>
        pBatchId == pRosterCardId
            ? PRosterTheme.PRosterSelectText
            : PRosterTheme.PRosterMutedBrush;
}

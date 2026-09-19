using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PRoster
{
    private UIElement PRosterCardBuild(IReadOnlyList<LWorkItem> pBatchItems, StackPanel pDetail)
    {
        DateTimeOffset pCreateTime = pBatchItems.Min(pWorkItem => pWorkItem.LWorkCreateTime);
        int pInitialCount = PRosterInitialRead(pBatchItems);
        string pTitle = pCreateTime.LocalDateTime.ToString(
            "yyyy-MM-dd tt h:mm", CultureInfo.CurrentUICulture);
        Guid pBatchId = pBatchItems[0].LWorkBatchId;

        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pTitleCell = new TextBlock
        {
            Text = pInitialCount == 1
                ? LLocalization.LLocalizationFormat("Roster.Card.One", pTitle)
                : LLocalization.LLocalizationFormat("Roster.Card.Many", pTitle, pInitialCount),
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = LRoster.LRosterCardCheck(pBatchId)
                ? PRosterTheme.PRosterSelectText
                : pRosterCompletedIds.Contains(pBatchId)
                    ? PRosterTheme.PRosterMutedBrush
                    : PRosterTheme.PRosterTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Cursor = Cursors.Hand
        };
        pRosterCardTitles[pBatchId] = pTitleCell;
        pTitleCell.MouseLeftButtonUp += (_, _) => PRosterCardSelect(pBatchId);
        Grid.SetColumn(pTitleCell, 0);
        pGrid.Children.Add(pTitleCell);

        var pControls = new StackPanel { Orientation = Orientation.Horizontal };
        pControls.Children.Add(PRosterMinimizeBuild(pBatchId, pDetail));
        pControls.Children.Add(PRosterCloseBuild(pBatchItems));
        Grid.SetColumn(pControls, 1);
        pGrid.Children.Add(pControls);

        var pHeader = new Border
        {
            Padding = new Thickness(12, 6, 6, 6),
            Background = LRoster.LRosterCardCheck(pBatchId)
                ? PRosterTheme.PRosterSelectCard
                : pRosterCompletedIds.Contains(pBatchId)
                    ? PRosterTheme.PRosterDoneCard
                    : PRosterTheme.PRosterCardBrush,
            BorderBrush = LRoster.LRosterCardCheck(pBatchId)
                ? PRosterTheme.PRosterSelectLine
                : pRosterCompletedIds.Contains(pBatchId)
                    ? PRosterTheme.PRosterDoneLine
                    : PRosterTheme.PRosterCardLine,
            Child = pGrid
        };
        pHeader.MouseEnter += (_, _) =>
        {
            if (!LRoster.LRosterCardCheck(pBatchId))
            {
                pHeader.Background = pRosterCompletedIds.Contains(pBatchId)
                    ? PRosterTheme.PRosterDoneHover
                    : PRosterTheme.PRosterHoverBrush;
            }
        };
        pHeader.MouseLeave += (_, _) => PRosterVisualApply(pBatchId, pHeader);
        pRosterCardHeaders[pBatchId] = pHeader;
        PRosterCollapseApply(pHeader, LRoster.LRosterCollapsedCheck(pBatchId));
        return pHeader;
    }

    private void PRosterCardSelect(Guid pBatchId)
    {
        LRoster.LRosterCardSelect(pBatchId);
        PRosterSelectApply();
        PRosterShadeApply();
        PRosterCardApply();
        PRosterSelectHandle();
    }

    private void PRosterCardRemove(IReadOnlyList<LWorkItem> pBatchItems)
    {
        IReadOnlyList<Guid> pRemovable = pRosterSchedule.LScheduleRemovableRead(
            pBatchItems.Select(pWorkItem => pWorkItem.LWorkId));
        if (pRemovable.Count == 0 || !PRosterCardConfirm(pRemovable.Count))
        {
            return;
        }

        PWindow.PWindowConsoleRead()?.LConsole.LConsoleRemovalRaise(pRosterSchedule.LScheduleBatchRemove(pRemovable));
    }

    private static bool PRosterCardConfirm(int pRemovableCount)
    {
        if (!LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive)
        {
            return true;
        }

        return PSAlert.PSAlertConfirm(
            null,
            LLocalization.LLocalizationTextRead("Console.Confirm.Title"),
            LLocalization.LLocalizationFormat("Roster.Card.Confirm", pRemovableCount),
            LLocalization.LLocalizationTextRead("Terms.Remove"));
    }
}

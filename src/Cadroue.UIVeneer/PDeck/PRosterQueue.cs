using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPanel;
using static Cadroue.UIVeneer.PPanel.PPanel;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PRoster
{
    private readonly Dictionary<Guid, PRosterRowCell> pRosterRowCells = new();
    private readonly Dictionary<Guid, Border> pRosterStepRows = new();
    private readonly Dictionary<Guid, PRosterRowPlace> pRosterRowPlaces = new();
    private readonly Dictionary<Guid, Border> pRosterCards = new();
    private readonly Dictionary<Guid, Border> pRosterCardHeaders = new();
    private readonly Dictionary<Guid, TextBlock> pRosterCardTitles = new();
    private readonly Dictionary<Guid, TextBlock> pRosterCloseGlyphs = new();
    private readonly Dictionary<Guid, PRosterBatchControl> pRosterBatchControls = new();
    private readonly HashSet<Guid> pRosterStageIds = new();
    private readonly HashSet<Guid> pRosterCompletedIds = new();

    private sealed class PRosterRowCell
    {
        public required TextBlock PRosterCellStep { get; init; }
        public required TextBlock PRosterCellDuration { get; init; }
        public required TextBlock PRosterCellProgress { get; init; }
        public required TextBlock PRosterCellPercent { get; init; }
        public required TextBlock PRosterCellState { get; init; }
        public required TextBlock PRosterCellOwner { get; init; }
    }

    private sealed record PRosterRowPlace(
        string PRosterPlaceSubject,
        long? PRosterOriginBytes,
        string PRosterPlaceStep,
        bool PRosterPlaceLast);

    private sealed record PRosterBatchControl(
        StackPanel PRosterBatchDetail,
        Border PRosterBatchButton,
        Image PRosterBatchIcon);

    private UIElement PRosterPanelBuild()
    {
        UIElement pOptions = PRosterOptionsBuild();
        var pColumnHeader = new Border
        {
            Padding = PRosterTheme.PRosterHeaderPadding,
            Background = PRosterTheme.PRosterHeaderBrush,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = PRosterHeaderBuild()
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pOptions, Dock.Top);
        DockPanel.SetDock(pColumnHeader, Dock.Top);
        pRoot.Children.Add(pOptions);
        pRoot.Children.Add(pColumnHeader);
        pRoot.Children.Add(pRosterQueueScroller);

        return PPanelBorderBuild(pRoot);
    }

    private ScrollViewer PRosterQueueBuild()
    {
        var pScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null,
            Padding = new Thickness(8, 8, 8, 8),
            Content = pRosterQueuePanel
        };
        PScrollbar.PScrollbarApply(pScroll);
        return pScroll;
    }

    private void PRosterQueueRebuild()
    {
        LWorkItem[] pItems = pRosterSchedule.LScheduleRecords.Where(PRosterVisibleCheck).ToArray();
        PRosterCompletedSync(pItems);
        IReadOnlyList<PRosterLineageEntry> pLineages = PRosterLineageRead(pItems);
        Guid[] pNextIds = pLineages
            .SelectMany(pLineage => pLineage.PRosterLineageItems)
            .Select(pWorkItem => pWorkItem.LWorkId)
            .ToArray();
        if (LRoster.LRosterOrderMatch(pNextIds))
        {
            foreach (PRosterLineageEntry pLineage in pLineages)
            {
                for (int pItemIndex = 0; pItemIndex < pLineage.PRosterLineageItems.Count; pItemIndex++)
                {
                    LWorkItem pWorkItem = pLineage.PRosterLineageItems[pItemIndex];
                    pRosterRowPlaces[pWorkItem.LWorkId] = new PRosterRowPlace(
                        pLineage.PRosterLineageSubject,
                        pLineage.PLineageOriginBytes,
                        PLineageStepRead(pWorkItem, pLineage.PRosterLineageSubject),
                        pItemIndex == pLineage.PRosterLineageItems.Count - 1);
                    if (pRosterStepRows.TryGetValue(pWorkItem.LWorkId, out Border? pRow))
                    {
                        pRow.Tag = pWorkItem;
                    }

                    PRosterRowUpdate(pWorkItem);
                }
            }

            return;
        }

        var pBatchOrder = new List<Guid>();
        var pBatchMap = new Dictionary<Guid, List<PRosterLineageEntry>>();
        foreach (PRosterLineageEntry pLineage in pLineages)
        {
            Guid pBatchId = pLineage.PRosterLineageItems[0].LWorkBatchId;
            if (!pBatchMap.TryGetValue(pBatchId, out List<PRosterLineageEntry>? pBatchLineages))
            {
                pBatchLineages = new List<PRosterLineageEntry>();
                pBatchMap[pBatchId] = pBatchLineages;
                pBatchOrder.Add(pBatchId);
            }

            pBatchLineages.Add(pLineage);
        }

        pRosterRowCells.Clear();
        pRosterRowPlaces.Clear();
        pRosterStepRows.Clear();
        pRosterRowBatch.Clear();
        pRosterFileShades.Clear();
        LRoster.LRosterOrderClear();
        pRosterCards.Clear();
        pRosterCardHeaders.Clear();
        pRosterCardTitles.Clear();
        pRosterCloseGlyphs.Clear();
        pRosterBatchControls.Clear();
        pRosterStageIds.Clear();
        pRosterQueuePanel.Children.Clear();

        foreach (Guid pBatchId in pBatchOrder)
        {
            pRosterQueuePanel.Children.Add(PRosterBatchBuild(pBatchMap[pBatchId]));
        }

        LRoster.LRosterStaleRemove(pBatchOrder.ToHashSet());
        PRosterShadeApply();
    }

    private void PRosterCompletedSync(IEnumerable<LWorkItem> pItems)
    {
        IGrouping<Guid, LWorkItem>[] pBatches = pItems
            .GroupBy(pWorkItem => pWorkItem.LWorkBatchId)
            .ToArray();
        HashSet<Guid> pPresentBatchIds = pBatches.Select(pBatch => pBatch.Key).ToHashSet();
        pRosterCompletedIds.RemoveWhere(pBatchId => !pPresentBatchIds.Contains(pBatchId));

        bool pCollapseCompleted = LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone;
        foreach (IGrouping<Guid, LWorkItem> pBatch in pBatches)
        {
            bool pCompleted = pBatch.All(pWorkItem => pWorkItem.LWorkStateCurrent is not
                (LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning));
            if (pCompleted)
            {
                pRosterCompletedIds.Add(pBatch.Key);
            }
            else
            {
                pRosterCompletedIds.Remove(pBatch.Key);
            }

            if (pCollapseCompleted)
            {
                LRoster.LRosterCollapseSet(pBatch.Key, pCompleted);
            }

            PRosterBatchApply(pBatch.Key, LRoster.LRosterCollapsedCheck(pBatch.Key));
        }
    }

    private Border PRosterBatchBuild(IReadOnlyList<PRosterLineageEntry> pLineages)
    {
        var pStack = new StackPanel();

        LWorkItem[] pBatchItems = pLineages.SelectMany(pLineage => pLineage.PRosterLineageItems).ToArray();
        Guid pBatchId = pBatchItems[0].LWorkBatchId;
        var pDetail = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 6),
            Visibility = LRoster.LRosterCollapsedCheck(pBatchId) ? Visibility.Collapsed : Visibility.Visible
        };
        pStack.Children.Add(PRosterCardBuild(pBatchItems, pDetail));

        HashSet<string> pConsumed = PRosterConsumedRead(pBatchItems);
        HashSet<string> pProduced = PRosterProducedRead(pBatchItems);
        foreach (LWorkItem pWorkItem in pBatchItems)
        {
            if (PRosterStageCheck(pWorkItem, pConsumed, pProduced))
            {
                pRosterStageIds.Add(pWorkItem.LWorkId);
            }
        }

        foreach (PRosterLineageEntry pLineage in pLineages)
        {
            List<LWorkItem> pStepItems = pLineage.PRosterLineageItems;
            bool pLineageStage = pStepItems.Count > 0 && pRosterStageIds.Contains(pStepItems[^1].LWorkId);
            Border pFileRow = PRosterFileBuild(pLineage, pLineageStage);
            pRosterFileShades.Add(new PRosterShade(pFileRow, pBatchId, pLineageStage));
            pDetail.Children.Add(pFileRow);

            for (int pItemIndex = 0; pItemIndex < pStepItems.Count; pItemIndex++)
            {
                LWorkItem pWorkItem = pStepItems[pItemIndex];
                bool pLast = pItemIndex == pStepItems.Count - 1;
                pRosterRowPlaces[pWorkItem.LWorkId] = new PRosterRowPlace(
                    pLineage.PRosterLineageSubject,
                    pLineage.PLineageOriginBytes,
                    PLineageStepRead(pWorkItem, pLineage.PRosterLineageSubject),
                    pLast);

                pRosterRowBatch[pWorkItem.LWorkId] = pBatchId;
                LRoster.LRosterOrderAdd(pWorkItem.LWorkId);
                pDetail.Children.Add(PRosterRowBuild(pWorkItem));
            }
        }

        pStack.Children.Add(pDetail);

        var pCard = new Border
        {
            CornerRadius = new CornerRadius(PRosterTheme.PRosterCorner),
            Background = pRosterCompletedIds.Contains(pBatchId)
                ? PRosterTheme.PRosterDoneBody
                : LRoster.LRosterCardCheck(pBatchId)
                    ? PRosterTheme.PRosterSelectBody
                    : PRosterTheme.PRosterBodyBrush,
            BorderBrush = pRosterCompletedIds.Contains(pBatchId)
                ? PRosterTheme.PRosterDoneLine
                : LRoster.LRosterCardCheck(pBatchId)
                    ? PRosterTheme.PRosterOuterLine
                    : PRosterTheme.PRosterCardLine,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 8),
            Child = pStack
        };
        pRosterCards[pBatchId] = pCard;
        return pCard;
    }

    private bool PRosterVisibleCheck(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning
            || pWorkItem.LWorkOwnerRunner == Guid.Empty
            || pRosterStation.LStationRunner.LRunnerOwnerCheck(pWorkItem))
        {
            return true;
        }

        return !LSentinel.LSentinelOwnerCheck(
            pWorkItem.LWorkOwnerProcess, pWorkItem.LWorkOwnerStamp, pWorkItem.LWorkOwnerRunner);
    }

}

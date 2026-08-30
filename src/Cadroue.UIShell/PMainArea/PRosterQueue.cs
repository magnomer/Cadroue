using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private readonly Dictionary<Guid, PRosterRowCell> pRosterRowCells = new();
    private readonly Dictionary<Guid, Border> pRosterStepRows = new();
    private readonly Dictionary<Guid, PRosterRowPlace> pRosterRowPlaces = new();
    private readonly List<Guid> pRosterOrderedIds = new();
    private readonly Dictionary<Guid, Border> pRosterCards = new();
    private readonly Dictionary<Guid, Border> pRosterCardHeaders = new();
    private readonly Dictionary<Guid, TextBlock> pRosterCardTitles = new();
    private readonly Dictionary<Guid, TextBlock> pRosterCloseGlyphs = new();
    private readonly Dictionary<Guid, PRosterBatchControl> pRosterBatchControls = new();
    private readonly HashSet<Guid> pRosterStageIds = new();
    private readonly HashSet<Guid> pRosterCollapsedIds = new();
    private readonly HashSet<Guid> pRosterCompletedIds = new();
    private readonly HashSet<Guid> pRosterSelectedIds = new();
    private Guid pRosterCurrentId;
    private Guid pRosterCardId;
    private CheckBox? pRosterSharedBox;
    private CheckBox? pRosterCompletedBox;

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

    private sealed record PRosterBatchControl(StackPanel PRosterBatchDetail, Border PRosterBatchButton, Image PRosterBatchIcon);

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

        return PPanel.PPanelBorderBuild(pRoot);
    }

    private UIElement PRosterOptionsBuild()
    {
        var pSharedToggle = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Roster.Queue.Shared"),
            FontSize = PRosterTheme.PRosterRowSize,
            IsChecked = LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared
        };
        PCheckbox.PCheckboxApply(pSharedToggle);
        pSharedToggle.Checked += (_, _) => PRosterSharedApply(true);
        pSharedToggle.Unchecked += (_, _) => PRosterSharedApply(false);
        pRosterSharedBox = pSharedToggle;

        var pCollapseToggle = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Roster.Queue.CollapseCompleted"),
            FontSize = PRosterTheme.PRosterRowSize,
            IsChecked = LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone,
            Margin = new Thickness(18, 0, 0, 0)
        };
        PCheckbox.PCheckboxApply(pCollapseToggle);
        pCollapseToggle.Checked += (_, _) => PRosterCompletedApply(true);
        pCollapseToggle.Unchecked += (_, _) => PRosterCompletedApply(false);
        pRosterCompletedBox = pCollapseToggle;

        var pOptions = new StackPanel { Orientation = Orientation.Horizontal };
        pOptions.Children.Add(pSharedToggle);
        pOptions.Children.Add(pCollapseToggle);

        return new Border
        {
            Padding = PRosterTheme.PRosterHeaderPadding,
            Background = PRosterTheme.PRosterHeaderBrush,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = pOptions
        };
    }

    private void PRosterSharedApply(bool pShared)
    {
        if (pShared == LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared)
        {
            return;
        }

        LPreferenceState pNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        pNext.LPreferenceWorklistShared = pShared;
        LPreference.LPreferenceStateSet(pNext);
        pRosterSchedule.LScheduleLoad();
    }

    private void PRosterCompletedApply(bool pCollapseCompleted)
    {
        if (pCollapseCompleted == LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone)
        {
            return;
        }

        LPreferenceState pNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        pNext.LPreferenceCollapseDone = pCollapseCompleted;
        LPreference.LPreferenceStateSet(pNext);
        if (pCollapseCompleted)
        {
            PRosterCompletedSync(pRosterSchedule.LScheduleRecords.Where(PRosterVisibleCheck));
        }
    }

    private static Grid PRosterHeaderBuild()
    {
        var pGrid = PRosterColumnsCreate();
        PRosterHeadAdd(pGrid, 0, LLocalization.LLocalizationTextRead("Roster.Queue.Step"));
        PRosterHeadAdd(pGrid, 1, LLocalization.LLocalizationTextRead("Roster.Queue.Priority"));
        PRosterHeadAdd(pGrid, 2, LLocalization.LLocalizationTextRead("Roster.Queue.Length"));
        PRosterHeadAdd(pGrid, 3, LLocalization.LLocalizationTextRead("Roster.Queue.Progress"));
        PRosterHeadAdd(pGrid, 4, LLocalization.LLocalizationTextRead("Roster.Queue.Percentage"));
        PRosterHeadAdd(pGrid, 5, LLocalization.LLocalizationTextRead("Roster.Queue.State"));
        PRosterHeadAdd(pGrid, 6, LLocalization.LLocalizationTextRead("Roster.Queue.Owner"));
        return pGrid;
    }

    private static void PRosterHeadAdd(Grid pGrid, int pColumn, string pText)
    {
        var pCell = new TextBlock
        {
            Text = pText,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterMutedBrush
        };
        Grid.SetColumn(pCell, pColumn);
        pGrid.Children.Add(pCell);
    }

    private static Grid PRosterColumnsCreate()
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 90 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 58 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 60 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 62 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 80 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 68 });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 84 });
        return pGrid;
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
        if (pRosterOrderedIds.SequenceEqual(pNextIds))
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
        pRosterOrderedIds.Clear();
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

        var pPresent = pRosterOrderedIds.ToHashSet();
        pRosterSelectedIds.RemoveWhere(pRosterId => !pPresent.Contains(pRosterId));
        if (!pPresent.Contains(pRosterCurrentId))
        {
            pRosterCurrentId = Guid.Empty;
        }

        if (pRosterCardId != Guid.Empty && !pBatchOrder.Contains(pRosterCardId))
        {
            pRosterCardId = Guid.Empty;
        }

        var pBatchPresent = pBatchOrder.ToHashSet();
        pRosterCollapsedIds.RemoveWhere(pBatchId => !pBatchPresent.Contains(pBatchId));
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
                if (pCompleted)
                {
                    pRosterCollapsedIds.Add(pBatch.Key);
                }
                else
                {
                    pRosterCollapsedIds.Remove(pBatch.Key);
                }
            }

            PRosterBatchApply(pBatch.Key, pRosterCollapsedIds.Contains(pBatch.Key));
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
            Visibility = pRosterCollapsedIds.Contains(pBatchId) ? Visibility.Collapsed : Visibility.Visible
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
            pRosterFileShades.Add((pFileRow, pBatchId, pLineageStage));
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
                pRosterOrderedIds.Add(pWorkItem.LWorkId);
                pDetail.Children.Add(PRosterRowBuild(pWorkItem));
            }
        }

        pStack.Children.Add(pDetail);

        var pCard = new Border
        {
            CornerRadius = new CornerRadius(PRosterTheme.PRosterCorner),
            Background = pRosterCompletedIds.Contains(pBatchId)
                ? PRosterTheme.PRosterDoneBody
                : pBatchId == pRosterCardId
                    ? PRosterTheme.PRosterSelectBody
                    : PRosterTheme.PRosterBodyBrush,
            BorderBrush = pRosterCompletedIds.Contains(pBatchId)
                ? PRosterTheme.PRosterDoneLine
                : pBatchId == pRosterCardId
                    ? PRosterTheme.PRosterOuterLine
                    : PRosterTheme.PRosterCardLine,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 8),
            Child = pStack
        };
        pRosterCards[pBatchId] = pCard;
        return pCard;
    }

    private void PRosterStepSelect(LWorkItem pWorkItem)
    {
        Guid pId = pWorkItem.LWorkId;
        pRosterCardId = Guid.Empty;
        PRosterCardApply();
        ModifierKeys pModifiers = Keyboard.Modifiers;

        if ((pModifiers & ModifierKeys.Shift) != 0 && pRosterCurrentId != Guid.Empty)
        {
            int pAnchor = pRosterOrderedIds.IndexOf(pRosterCurrentId);
            int pTarget = pRosterOrderedIds.IndexOf(pId);
            if (pAnchor >= 0 && pTarget >= 0)
            {
                pRosterSelectedIds.Clear();
                for (int pIndex = Math.Min(pAnchor, pTarget); pIndex <= Math.Max(pAnchor, pTarget); pIndex++)
                {
                    pRosterSelectedIds.Add(pRosterOrderedIds[pIndex]);
                }
            }
        }
        else if ((pModifiers & ModifierKeys.Control) != 0)
        {
            if (!pRosterSelectedIds.Add(pId))
            {
                pRosterSelectedIds.Remove(pId);
            }

            pRosterCurrentId = pId;
        }
        else
        {
            pRosterSelectedIds.Clear();
            pRosterSelectedIds.Add(pId);
            pRosterCurrentId = pId;
        }

        PRosterSelectApply();
        PRosterShadeApply();
        PRosterSelectHandle();
    }

    private void PRosterHoverApply(Guid pId, bool pOver)
    {
        if (pRosterSelectedIds.Contains(pId) || !pRosterStepRows.TryGetValue(pId, out Border? pRow))
        {
            return;
        }

        pRow.Background = pOver ? PRosterTheme.PRosterHeaderBrush : PRosterShadeRead(pId);
    }

    private void PRosterSelectApply()
    {
        foreach ((Guid pRowId, Border pRow) in pRosterStepRows)
        {
            pRow.Background = pRosterSelectedIds.Contains(pRowId)
                ? PRosterTheme.PRosterSelectBrush
                : PRosterShadeRead(pRowId);
        }
    }

    private string PRosterStepRead(LWorkItem pWorkItem) =>
        pRosterRowPlaces.TryGetValue(pWorkItem.LWorkId, out PRosterRowPlace? pPlace)
            ? pPlace.PRosterPlaceStep
            : pWorkItem.LWorkOutputName;

    private string PRosterPlaceFormat(LWorkItem pWorkItem) =>
        pRosterRowPlaces.TryGetValue(pWorkItem.LWorkId, out PRosterRowPlace? pPlace)
            ? PLineageRatioFormat(pWorkItem, pPlace.PRosterPlaceSubject, pPlace.PRosterOriginBytes)
            : PRosterRatioFormat(pWorkItem);

    private string PRosterOwnerFormat(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkOwnerRunner == Guid.Empty)
        {
            return "-";
        }

        if (pRosterStation.LStationRunner.LRunnerOwnerCheck(pWorkItem))
        {
            return LLocalization.LLocalizationTextRead("Roster.Owner.ThisTab");
        }

        return pWorkItem.LWorkOwnerProcess == Environment.ProcessId
            ? LLocalization.LLocalizationTextRead("Roster.Owner.OtherTab")
            : LLocalization.LLocalizationTextRead("Roster.Owner.OtherWindow");
    }

    private static string PRosterProgressFormat(LWorkItem pWorkItem) =>
        pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning || pWorkItem.LWorkProgress > 0
            ? $"{pWorkItem.LWorkProgress:P0}"
            : "-";

    private static string PRosterPriorityFormat(LWorkPriority pPriority) =>
        pPriority == LWorkPriority.LWorkPriorityHigh
            ? LLocalization.LLocalizationTextRead("Roster.Priority.High")
            : LLocalization.LLocalizationTextRead("Roster.Priority.Normal");

    private static string PRosterSpanFormat(TimeSpan pSpan) => $"{pSpan:hh\\:mm\\:ss}";

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

    private LWorkItem? PRosterSelectRead() =>
        pRosterCurrentId == Guid.Empty
            ? null
            : pRosterSchedule.LScheduleRecords.FirstOrDefault(pWorkItem => pWorkItem.LWorkId == pRosterCurrentId);

    private IReadOnlyList<LWorkItem> PRosterSelectionRead() =>
        pRosterSchedule.LScheduleRecords
            .Where(pWorkItem => pRosterSelectedIds.Contains(pWorkItem.LWorkId))
            .ToArray();
}

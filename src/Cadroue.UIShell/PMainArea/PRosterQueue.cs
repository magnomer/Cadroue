using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    private readonly Dictionary<Guid, Border> pRosterCardHeaders = new();
    private readonly HashSet<Guid> pRosterStageIds = new();
    private readonly HashSet<Guid> pRosterCollapsedIds = new();
    private readonly HashSet<Guid> pRosterSelectedIds = new();
    private Guid pRosterCurrentId;
    private Guid pRosterCardId;

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

    private UIElement PRosterPanelBuild()
    {
        var pColumnHeader = new Border
        {
            Padding = PRosterTheme.PRosterHeaderPadding,
            Background = PRosterTheme.PRosterHeaderBrush,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = PRosterHeaderBuild()
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pColumnHeader, Dock.Top);
        pRoot.Children.Add(pColumnHeader);
        pRoot.Children.Add(pRosterQueueScroller);

        return PPanel.PPanelBorderBuild(pRoot);
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
        IReadOnlyList<PRosterLineageEntry> pLineages = PRosterLineageRead(pItems);

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
        pRosterOrderedIds.Clear();
        pRosterCardHeaders.Clear();
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
    }

    private Border PRosterBatchBuild(IReadOnlyList<PRosterLineageEntry> pLineages)
    {
        var pStack = new StackPanel();

        LWorkItem[] pBatchItems = pLineages.SelectMany(pLineage => pLineage.PRosterLineageItems).ToArray();
        Guid pBatchId = pBatchItems[0].LWorkBatchId;
        var pDetail = new StackPanel
        {
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
            pDetail.Children.Add(PRosterFileBuild(pLineage, pLineageStage));

            for (int pItemIndex = 0; pItemIndex < pStepItems.Count; pItemIndex++)
            {
                LWorkItem pWorkItem = pStepItems[pItemIndex];
                bool pLast = pItemIndex == pStepItems.Count - 1;
                pRosterRowPlaces[pWorkItem.LWorkId] = new PRosterRowPlace(
                    pLineage.PRosterLineageSubject,
                    pLineage.PLineageOriginBytes,
                    PLineageStepRead(pWorkItem, pLineage.PRosterLineageSubject),
                    pLast);

                pRosterOrderedIds.Add(pWorkItem.LWorkId);
                pDetail.Children.Add(PRosterRowBuild(pWorkItem));
            }
        }

        pStack.Children.Add(pDetail);

        return new Border
        {
            CornerRadius = new CornerRadius(PRosterTheme.PRosterCorner),
            Background = Brushes.White,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(0, 0, 0, 6),
            Margin = new Thickness(0, 0, 0, 8),
            Child = pStack
        };
    }

    private static Border PRosterFileBuild(PRosterLineageEntry pLineage, bool pStage) => new()
    {
        Background = pStage ? PRosterTheme.PRosterStageBrush : Brushes.Transparent,
        Padding = new Thickness(12, 5, 12, 3),
        Child = new TextBlock
        {
            Text = PLineageTitleRead(pLineage),
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterTitleBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        }
    };

    private Brush PRosterShadeRead(Guid pRowId) =>
        pRosterStageIds.Contains(pRowId) ? PRosterTheme.PRosterStageBrush : Brushes.Transparent;

    private Border PRosterRowBuild(LWorkItem pWorkItem)
    {
        var pGrid = PRosterColumnsCreate();
        pGrid.MinHeight = 29;

        TextBlock pStepCell = PRosterStepAdd(pGrid, pWorkItem);
        PRosterCellAdd(pGrid, 1, PRosterPriorityFormat(pWorkItem.LWorkPriority), PRosterTheme.PRosterMutedBrush);
        TextBlock pDurationCell = PRosterCellAdd(pGrid, 2, PRosterSpanFormat(pWorkItem.LWorkDuration), PRosterTheme.PRosterMutedBrush);

        TextBlock pProgressCell = PRosterCellAdd(pGrid, 3, PRosterProgressFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);
        TextBlock pPercentCell = PRosterCellAdd(pGrid, 4, PRosterPlaceFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);
        TextBlock pStateCell = PRosterCellAdd(pGrid, 5, PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent),
            PRosterTheme.PRosterStateRead(pWorkItem.LWorkStateCurrent));
        TextBlock pOwnerCell = PRosterCellAdd(pGrid, 6, PRosterOwnerFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);

        pRosterRowCells[pWorkItem.LWorkId] = new PRosterRowCell
        {
            PRosterCellStep = pStepCell,
            PRosterCellDuration = pDurationCell,
            PRosterCellProgress = pProgressCell,
            PRosterCellPercent = pPercentCell,
            PRosterCellState = pStateCell,
            PRosterCellOwner = pOwnerCell
        };

        var pRow = new Border
        {
            Padding = new Thickness(12, 0, 12, 0),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = pGrid,
            Tag = pWorkItem,
            ContextMenu = PMenu.PMenuContextCreate()
        };

        Guid pRowId = pWorkItem.LWorkId;
        pRow.PreviewMouseLeftButtonDown += (_, _) => PRosterStepSelect(pWorkItem);
        pRow.MouseEnter += (_, _) => PRosterHoverApply(pRowId, true);
        pRow.MouseLeave += (_, _) => PRosterHoverApply(pRowId, false);
        pRow.ContextMenuOpening += (_, pArgs) => PRosterMenuOpen(pRow, pArgs);

        pRosterStepRows[pRowId] = pRow;
        pRow.Background = pRosterSelectedIds.Contains(pRowId)
            ? PRosterTheme.PRosterSelectBrush
            : PRosterShadeRead(pRowId);
        return pRow;
    }

    private TextBlock PRosterStepAdd(Grid pGrid, LWorkItem pWorkItem)
    {
        bool pLast = !pRosterRowPlaces.TryGetValue(pWorkItem.LWorkId, out PRosterRowPlace? pPlace) || pPlace.PRosterPlaceLast;

        var pStepGrid = new Grid();
        pStepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pStepGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        UIElement pConnector = PRosterConnectorBuild(pLast, pRosterStageIds.Contains(pWorkItem.LWorkId));
        Grid.SetColumn(pConnector, 0);
        pStepGrid.Children.Add(pConnector);

        var pStepCell = new TextBlock
        {
            Text = PRosterStepRead(pWorkItem),
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = PRosterTheme.PRosterTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(pStepCell, 1);
        pStepGrid.Children.Add(pStepCell);

        Grid.SetColumn(pStepGrid, 0);
        pGrid.Children.Add(pStepGrid);
        return pStepCell;
    }

    private static UIElement PRosterConnectorBuild(bool pLast, bool pStage)
    {
        Brush pSpineBrush = pStage ? PRosterTheme.PRosterMutedBrush : PRosterTheme.PRosterTrunkBrush;
        var pGrid = new Grid { Width = 18, UseLayoutRounding = true };
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var pSpineUpper = PRosterSpineBuild(pSpineBrush);
        Grid.SetRow(pSpineUpper, 0);
        pGrid.Children.Add(pSpineUpper);

        if (!pLast)
        {
            var pSpineLower = PRosterSpineBuild(pSpineBrush);
            Grid.SetRow(pSpineLower, 1);
            pGrid.Children.Add(pSpineLower);
        }

        var pNode = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = pStage ? PRosterTheme.PRosterTextBrush : Brushes.White,
            BorderBrush = pStage ? PRosterTheme.PRosterTextBrush : PRosterTheme.PRosterAccentBrush,
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };
        Grid.SetRowSpan(pNode, 2);
        pGrid.Children.Add(pNode);

        return pGrid;
    }

    private static Border PRosterSpineBuild(Brush pSpineBrush) => new()
    {
        Width = 2,
        Background = pSpineBrush,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Stretch,
        Margin = new Thickness(7, 0, 0, 0)
    };

    private static TextBlock PRosterCellAdd(Grid pGrid, int pColumn, string pText, Brush pBrush)
    {
        var pCell = new TextBlock
        {
            Text = pText,
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = pBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(pCell, pColumn);
        pGrid.Children.Add(pCell);
        return pCell;
    }

    private void PRosterRowUpdate(LWorkItem pWorkItem)
    {
        if (!pRosterRowCells.TryGetValue(pWorkItem.LWorkId, out PRosterRowCell? pCell))
        {
            return;
        }

        pCell.PRosterCellStep.Text = PRosterStepRead(pWorkItem);
        pCell.PRosterCellDuration.Text = PRosterSpanFormat(pWorkItem.LWorkDuration);
        pCell.PRosterCellProgress.Text = PRosterProgressFormat(pWorkItem);
        pCell.PRosterCellPercent.Text = PRosterPlaceFormat(pWorkItem);
        pCell.PRosterCellState.Text = PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent);
        pCell.PRosterCellState.Foreground = PRosterTheme.PRosterStateRead(pWorkItem.LWorkStateCurrent);
        pCell.PRosterCellOwner.Text = PRosterOwnerFormat(pWorkItem);
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

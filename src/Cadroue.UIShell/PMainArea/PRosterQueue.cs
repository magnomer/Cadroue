using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private readonly Dictionary<Guid, PRosterRowCell> pRosterRowCells = new();
    private readonly Dictionary<Guid, ListBoxItem> pRosterRows = new();
    private readonly Dictionary<Guid, PRosterRowPlace> pRosterRowPlaces = new();
    private bool pRosterQueueSyncing;

    private sealed class PRosterRowCell
    {
        public required TextBlock PRosterCellStep { get; init; }
        public required TextBlock PRosterCellProgress { get; init; }
        public required TextBlock PRosterCellPercent { get; init; }
        public required TextBlock PRosterCellState { get; init; }
        public required TextBlock PRosterCellOwner { get; init; }
    }

    private sealed record PRosterRowPlace(
        string PRosterPlaceSubject,
        long? PRosterPlaceOriginBytes,
        string PRosterPlaceStep);

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
        pRoot.Children.Add(pRosterQueueList);

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

    private ListBox PRosterQueueBuild()
    {
        var pList = new ListBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.White,
            FocusVisualStyle = null,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            SelectionMode = SelectionMode.Extended,
            ItemContainerStyle = PRosterStyleCreate()
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(pList, ScrollBarVisibility.Disabled);
        pList.SelectionChanged += (_, _) => PRosterSelectHandle();
        return pList;
    }

    private static Style PRosterStyleCreate()
    {
        var pStyle = new Style(typeof(ListBoxItem));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PRosterTemplateCreate()));
        return pStyle;
    }

    private static ControlTemplate PRosterTemplateCreate()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "pRosterRowFrame";
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.BorderBrushProperty, PRosterTheme.PRosterLineBrush);
        pBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
        pBorder.SetValue(Border.PaddingProperty, PRosterTheme.PRosterRowPadding);

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pBorder.AppendChild(pContent);

        var pTemplate = new ControlTemplate(typeof(ListBoxItem)) { VisualTree = pBorder };

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, PRosterTheme.PRosterHeaderBrush, "pRosterRowFrame"));
        pTemplate.Triggers.Add(pHover);

        var pSelected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        pSelected.Setters.Add(new Setter(Border.BackgroundProperty, PRosterTheme.PRosterSelectBrush, "pRosterRowFrame"));
        pTemplate.Triggers.Add(pSelected);

        return pTemplate;
    }

    private void PRosterQueueRebuild()
    {
        var pSelectedIds = pRosterQueueList.SelectedItems
            .OfType<ListBoxItem>()
            .Select(pRow => pRow.Tag)
            .OfType<LWorkItem>()
            .Select(pWorkItem => pWorkItem.LWorkId)
            .ToHashSet();

        LWorkItem[] pDesired = pRosterSchedule.LScheduleRecords.ToArray();
        var pDesiredIds = pDesired.Select(pWorkItem => pWorkItem.LWorkId).ToHashSet();
        List<ListBoxItem> pDesiredRows = PRosterRowsRead(pDesired);

        pRosterQueueSyncing = true;
        try
        {
            foreach (Guid pStaleId in pRosterRows.Keys.Where(pRowId => !pDesiredIds.Contains(pRowId)).ToArray())
            {
                pRosterRowCells.Remove(pStaleId);
                pRosterRows.Remove(pStaleId);
                pRosterRowPlaces.Remove(pStaleId);
            }

            var pDesiredSet = pDesiredRows.ToHashSet();
            for (int pIndex = pRosterQueueList.Items.Count - 1; pIndex >= 0; pIndex--)
            {
                if (pRosterQueueList.Items[pIndex] is not ListBoxItem pExistingRow
                    || !pDesiredSet.Contains(pExistingRow))
                {
                    pRosterQueueList.Items.RemoveAt(pIndex);
                }
            }

            for (int pIndex = 0; pIndex < pDesiredRows.Count; pIndex++)
            {
                ListBoxItem pRow = pDesiredRows[pIndex];
                int pCurrentIndex = pRosterQueueList.Items.IndexOf(pRow);
                if (pCurrentIndex == pIndex)
                {
                    continue;
                }

                if (pCurrentIndex >= 0)
                {
                    pRosterQueueList.Items.RemoveAt(pCurrentIndex);
                }

                pRosterQueueList.Items.Insert(Math.Min(pIndex, pRosterQueueList.Items.Count), pRow);
            }

            foreach (ListBoxItem pRow in pRosterQueueList.Items.OfType<ListBoxItem>())
            {
                bool pSelected = pRow.Tag is LWorkItem pWorkItem && pSelectedIds.Contains(pWorkItem.LWorkId);
                if (pRow.IsSelected != pSelected)
                {
                    pRow.IsSelected = pSelected;
                }
            }
        }
        finally
        {
            pRosterQueueSyncing = false;
        }
    }

    private List<ListBoxItem> PRosterRowsRead(IReadOnlyList<LWorkItem> pWorkItems)
    {
        var pDesiredRows = new List<ListBoxItem>();
        var pLineageKeep = new HashSet<Guid>();

        foreach (var pLineageEntry in PRosterLineageRead(pWorkItems))
        {
            pLineageKeep.Add(pLineageEntry.PRosterLineageId);
            pDesiredRows.Add(PLineageRowRead(pLineageEntry));

            for (int pItemIndex = 0; pItemIndex < pLineageEntry.PRosterLineageItems.Count; pItemIndex++)
            {
                LWorkItem pWorkItem = pLineageEntry.PRosterLineageItems[pItemIndex];
                pRosterRowPlaces[pWorkItem.LWorkId] = new PRosterRowPlace(
                    pLineageEntry.PRosterLineageSubject,
                    pLineageEntry.PLineageOriginBytes,
                    PLineageStepRead(pWorkItem, pLineageEntry.PRosterLineageSubject, pItemIndex == 0));

                if (pRosterRows.TryGetValue(pWorkItem.LWorkId, out ListBoxItem? pRow))
                {
                    pRow.Tag = pWorkItem;
                    PRosterRowUpdate(pWorkItem);
                }
                else
                {
                    pRow = PRosterRowBuild(pWorkItem);
                    pRosterRows[pWorkItem.LWorkId] = pRow;
                }

                pDesiredRows.Add(pRow);
            }
        }

        PRosterLineageRemove(pLineageKeep);
        return pDesiredRows;
    }

    private ListBoxItem PRosterRowBuild(LWorkItem pWorkItem)
    {
        var pGrid = PRosterColumnsCreate();
        pGrid.Margin = new Thickness(PRosterLineageIndent, 0, 0, 0);

        TextBlock pStepCell = PRosterCellAdd(pGrid, 0, PRosterStepRead(pWorkItem), PRosterTheme.PRosterTextBrush);
        PRosterCellAdd(pGrid, 1, PRosterPriorityFormat(pWorkItem.LWorkPriority), PRosterTheme.PRosterMutedBrush);
        PRosterCellAdd(pGrid, 2, PRosterSpanFormat(pWorkItem.LWorkDuration), PRosterTheme.PRosterMutedBrush);

        TextBlock pProgressCell = PRosterCellAdd(pGrid, 3, PRosterProgressFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);
        TextBlock pPercentCell = PRosterCellAdd(pGrid, 4, PRosterPlaceFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);
        TextBlock pStateCell = PRosterCellAdd(pGrid, 5, PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent),
            PRosterTheme.PRosterStateRead(pWorkItem.LWorkStateCurrent));
        TextBlock pOwnerCell = PRosterCellAdd(pGrid, 6, PRosterOwnerFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);

        pRosterRowCells[pWorkItem.LWorkId] = new PRosterRowCell
        {
            PRosterCellStep = pStepCell,
            PRosterCellProgress = pProgressCell,
            PRosterCellPercent = pPercentCell,
            PRosterCellState = pStateCell,
            PRosterCellOwner = pOwnerCell
        };

        var pRow = new ListBoxItem { Content = pGrid, Tag = pWorkItem, ContextMenu = PMenu.PMenuContextCreate() };
        pRow.ContextMenuOpening += (_, pArgs) => PRosterMenuOpen(pRow, pArgs);
        return pRow;
    }

    private void PRosterMenuOpen(ListBoxItem pRow, ContextMenuEventArgs pArgs)
    {
        if (pRow.Tag is not LWorkItem pWorkItem || pRow.ContextMenu is not { } pMenu)
        {
            pArgs.Handled = true;
            return;
        }

        if (pWorkItem.LWorkStateCurrent is LWorkState.LWorkStateCancelled or LWorkState.LWorkStateFailed)
        {
            PRosterRestartMenuBuild(pMenu, pWorkItem);
            return;
        }

        IReadOnlyList<string> pRelayPaths = PRosterPathsRead(pWorkItem);
        if (pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateDone
            || pRelayPaths.Count == 0
            || LTabset.LTabsetCurrent is not { } pTabset)
        {
            pArgs.Handled = true;
            return;
        }

        pMenu.Items.Clear();
        MenuItem pHeader = PMenu.PMenuItemCreate(
            pRelayPaths.Count > 1
                ? LLocalization.LLocalizationFormat("Roster.Relay.Many", pRelayPaths.Count)
                : LLocalization.LLocalizationTextRead("Roster.Relay.One"), null);
        pHeader.IsEnabled = false;
        pMenu.Items.Add(pHeader);

        bool pAnyTarget = false;
        foreach (PTabRecord pTabRecord in pTabset.PTabsetRecords)
        {
            if (pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            pAnyTarget = true;
            PTabRecord pTargetRecord = pTabRecord;
            MenuItem pItem = PMenu.PMenuItemCreate(pTabRecord.PTabTitle, pTabRecord.PTabIconSource);
            pItem.Click += (_, _) => PRosterRelaySend(pTargetRecord, pRelayPaths);
            pMenu.Items.Add(pItem);
        }

        if (!pAnyTarget)
        {
            pArgs.Handled = true;
        }
    }

    private void PRosterRestartMenuBuild(ContextMenu pMenu, LWorkItem pClickedItem)
    {
        LWorkItem[] pRestartItems = PRosterSelectionRead()
            .Where(pItem => pItem.LWorkStateCurrent is LWorkState.LWorkStateCancelled or LWorkState.LWorkStateFailed)
            .ToArray();
        if (pRestartItems.Length == 0 || !pRestartItems.Any(pItem => ReferenceEquals(pItem, pClickedItem)))
        {
            pRestartItems = new[] { pClickedItem };
        }

        pMenu.Items.Clear();
        MenuItem pRestart = PMenu.PMenuItemCreate(
            pRestartItems.Length > 1
                ? LLocalization.LLocalizationFormat("Roster.Menu.RestartMany", pRestartItems.Length)
                : LLocalization.LLocalizationTextRead("Roster.Menu.Restart"),
            null);
        LWorkItem[] pRestartTargets = pRestartItems;
        pRestart.Click += (_, _) =>
        {
            foreach (LWorkItem pRestartItem in pRestartTargets)
            {
                LSchedule.LScheduleCurrent.LScheduleItemReset(pRestartItem.LWorkId);
            }
        };
        pMenu.Items.Add(pRestart);
    }

    private IReadOnlyList<string> PRosterPathsRead(LWorkItem pClickedItem)
    {
        IReadOnlyList<LWorkItem> pSelectedItems = PRosterSelectionRead();
        IEnumerable<LWorkItem> pRelayItems =
            pSelectedItems.Count > 1 && pSelectedItems.Any(pItem => ReferenceEquals(pItem, pClickedItem))
                ? pSelectedItems
                : new[] { pClickedItem };

        var pRelayPaths = new List<string>();
        foreach (LWorkItem pRelayItem in pRelayItems)
        {
            if (pRelayItem.LWorkStateCurrent == LWorkState.LWorkStateDone
                && PRosterFileRead(pRelayItem) is { } pRelayPath
                && !pRelayPaths.Contains(pRelayPath, StringComparer.OrdinalIgnoreCase))
            {
                pRelayPaths.Add(pRelayPath);
            }
        }

        return pRelayPaths;
    }

    private static string? PRosterFileRead(LWorkItem pWorkItem)
    {
        if (!string.IsNullOrWhiteSpace(pWorkItem.LWorkOutputPath) && File.Exists(pWorkItem.LWorkOutputPath))
        {
            return pWorkItem.LWorkOutputPath;
        }

        return !string.IsNullOrWhiteSpace(pWorkItem.LWorkSourcePath) && File.Exists(pWorkItem.LWorkSourcePath)
            ? pWorkItem.LWorkSourcePath
            : null;
    }

    private static void PRosterRelaySend(PTabRecord pTargetRecord, IReadOnlyList<string> pRelayPaths)
    {
        if (pTargetRecord.PTabWorkspace.PWorkspaceSurface.PTabList is not { } pTargetList)
        {
            return;
        }

        LTabset.LTabsetCurrent?.LTabsetSelect(pTargetRecord);
        pTargetList.PListClear();
        pTargetList.PListPathsAdd(pRelayPaths);
    }

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
        pCell.PRosterCellProgress.Text = PRosterProgressFormat(pWorkItem);
        pCell.PRosterCellPercent.Text = PRosterPlaceFormat(pWorkItem);
        pCell.PRosterCellState.Text = PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent);
        pCell.PRosterCellState.Foreground = PRosterTheme.PRosterStateRead(pWorkItem.LWorkStateCurrent);
        pCell.PRosterCellOwner.Text = PRosterOwnerFormat(pWorkItem);
    }

    private string PRosterStepRead(LWorkItem pWorkItem) =>
        pRosterRowPlaces.TryGetValue(pWorkItem.LWorkId, out PRosterRowPlace? pPlace)
            ? pPlace.PRosterPlaceStep
            : pWorkItem.LWorkOutputName;

    private string PRosterPlaceFormat(LWorkItem pWorkItem) =>
        pRosterRowPlaces.TryGetValue(pWorkItem.LWorkId, out PRosterRowPlace? pPlace)
            ? PLineageRatioFormat(pWorkItem, pPlace.PRosterPlaceSubject, pPlace.PRosterPlaceOriginBytes)
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

    private LWorkItem? PRosterSelectRead() =>
        pRosterQueueList.SelectedItem is ListBoxItem { Tag: LWorkItem pWorkItem } ? pWorkItem : null;

    private IReadOnlyList<LWorkItem> PRosterSelectionRead() =>
        pRosterQueueList.SelectedItems
            .OfType<ListBoxItem>()
            .Select(pRow => pRow.Tag)
            .OfType<LWorkItem>()
            .ToArray();
}

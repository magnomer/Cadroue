using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private readonly Dictionary<Guid, PRosterRowCell> pRosterRowCells = new();

    private sealed class PRosterRowCell
    {
        public required TextBlock PRosterCellProgress { get; init; }
        public required TextBlock PRosterCellState { get; init; }
        public required TextBlock PRosterCellOwner { get; init; }
    }

    private UIElement PRosterQueuePanelBuild()
    {
        var pColumnHeader = new Border
        {
            Padding = PRosterTheme.PRosterHeaderPadding,
            Background = PRosterTheme.PRosterHeaderBrush,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = PRosterColumnHeaderBuild()
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pColumnHeader, Dock.Top);
        pRoot.Children.Add(pColumnHeader);
        pRoot.Children.Add(pRosterQueueList);

        return PPanel.PPanelBorderBuild(pRoot);
    }

    private static Grid PRosterColumnHeaderBuild()
    {
        var pGrid = PRosterColumnsCreate();
        PRosterHeadCellAdd(pGrid, 0, "Output");
        PRosterHeadCellAdd(pGrid, 1, "Priority");
        PRosterHeadCellAdd(pGrid, 2, "Length");
        PRosterHeadCellAdd(pGrid, 3, "Progress");
        PRosterHeadCellAdd(pGrid, 4, "State");
        PRosterHeadCellAdd(pGrid, 5, "Owner");
        return pGrid;
    }

    private static void PRosterHeadCellAdd(Grid pGrid, int pColumn, string pText)
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
            ItemContainerStyle = PRosterRowStyleCreate()
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(pList, ScrollBarVisibility.Disabled);
        pList.SelectionChanged += (_, _) => PRosterSelectHandle();
        return pList;
    }

    private static Style PRosterRowStyleCreate()
    {
        var pStyle = new Style(typeof(ListBoxItem));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PRosterRowTemplateCreate()));
        return pStyle;
    }

    private static ControlTemplate PRosterRowTemplateCreate()
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
        Guid pSelectedId = pRosterQueueList.SelectedItem is ListBoxItem { Tag: LWorkItem pPrevious }
            ? pPrevious.LWorkId
            : Guid.Empty;

        pRosterRowCells.Clear();
        pRosterQueueList.Items.Clear();

        ListBoxItem? pSelectedRow = null;
        foreach (LWorkItem pWorkItem in pRosterSchedule.LScheduleRecords)
        {
            ListBoxItem pRow = PRosterRowBuild(pWorkItem);
            pRosterQueueList.Items.Add(pRow);
            if (pWorkItem.LWorkId == pSelectedId)
            {
                pSelectedRow = pRow;
            }
        }

        if (pSelectedRow is not null)
        {
            pRosterQueueList.SelectedItem = pSelectedRow;
        }
    }

    private ListBoxItem PRosterRowBuild(LWorkItem pWorkItem)
    {
        var pGrid = PRosterColumnsCreate();

        PRosterRowCellAdd(pGrid, 0, pWorkItem.LWorkOutputName, PRosterTheme.PRosterTextBrush);
        PRosterRowCellAdd(pGrid, 1, PRosterPriorityFormat(pWorkItem.LWorkPriority), PRosterTheme.PRosterMutedBrush);
        PRosterRowCellAdd(pGrid, 2, PRosterSpanFormat(pWorkItem.LWorkDuration), PRosterTheme.PRosterMutedBrush);

        TextBlock pProgressCell = PRosterRowCellAdd(pGrid, 3, PRosterProgressFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);
        TextBlock pStateCell = PRosterRowCellAdd(pGrid, 4, PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent),
            PRosterTheme.PRosterStateBrushRead(pWorkItem.LWorkStateCurrent));
        TextBlock pOwnerCell = PRosterRowCellAdd(pGrid, 5, PRosterOwnerFormat(pWorkItem), PRosterTheme.PRosterMutedBrush);

        pRosterRowCells[pWorkItem.LWorkId] = new PRosterRowCell
        {
            PRosterCellProgress = pProgressCell,
            PRosterCellState = pStateCell,
            PRosterCellOwner = pOwnerCell
        };

        return new ListBoxItem { Content = pGrid, Tag = pWorkItem };
    }

    private static TextBlock PRosterRowCellAdd(Grid pGrid, int pColumn, string pText, Brush pBrush)
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

        pCell.PRosterCellProgress.Text = PRosterProgressFormat(pWorkItem);
        pCell.PRosterCellState.Text = PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent);
        pCell.PRosterCellState.Foreground = PRosterTheme.PRosterStateBrushRead(pWorkItem.LWorkStateCurrent);
        pCell.PRosterCellOwner.Text = PRosterOwnerFormat(pWorkItem);
    }

    private string PRosterOwnerFormat(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkOwnerRunner == Guid.Empty)
        {
            return "-";
        }

        if (pRosterStation.LStationRunner.LRunnerOwnerCheck(pWorkItem))
        {
            return "This tab";
        }

        return pWorkItem.LWorkOwnerProcess == Environment.ProcessId ? "Other tab" : "Other window";
    }

    private static string PRosterProgressFormat(LWorkItem pWorkItem) =>
        pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning || pWorkItem.LWorkProgress > 0
            ? $"{pWorkItem.LWorkProgress:P0}"
            : "-";

    private static string PRosterPriorityFormat(LWorkPriority pPriority) =>
        pPriority == LWorkPriority.LWorkPriorityHigh ? "High" : "Normal";

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

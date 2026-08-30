using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PList
{
    private void PListRowsRebuild()
    {
        pListRowPanel.Children.Clear();
        pListRows.Clear();
        var pListGroupsShown = new HashSet<Guid>();
        IReadOnlyList<LDocketEntry> pListEntries = pListDocket.LDocketItemsRead();
        foreach (LDocketEntry pListItem in pListEntries)
        {
            if (!pListItem.LDocketEntryLocked)
            {
                pListRowPanel.Children.Add(PListRowBuild(pListItem));
                continue;
            }

            if (pListGroupsShown.Add(pListItem.LDocketEntryBatch))
            {
                LDocketEntry[] pListGroupItems = pListEntries
                    .Where(pCandidate => pCandidate.LDocketEntryLocked
                        && pCandidate.LDocketEntryBatch == pListItem.LDocketEntryBatch)
                    .ToArray();
                pListRowPanel.Children.Add(PListCardBuild(pListGroupItems));
            }
        }

        PListEmptyUpdate();
    }

    private Border PListRowBuild(LDocketEntry pListItem, bool pListBottomBorder = true)
    {
        string pRowPath = pListItem.LDocketEntryPath;
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead("/PAssets/PPanels/PVideo.svg", pListItem.LDocketEntryLocked ? pListMutedBrush : pListIconBrush),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(pRowPath),
            FontSize = 12,
            FontFamily = pListFontFamily,
            Foreground = pListItem.LDocketEntryLocked ? pListMutedBrush : pListRowBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = PListBackgroundRead(pListItem),
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(0, 0, 0, pListBottomBorder ? 1 : 0),
            Cursor = Cursors.Hand,
            ToolTip = pListItem.LDocketEntryLocked
                ? $"{pRowPath}\n{LLocalization.LLocalizationTextRead("List.Locked.Tooltip")}" : pRowPath,
            Child = pRowContent,
            Tag = pRowPath
        };
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) =>
        {
            Focus();
            PListPressHandle(pRowPath, pRowEvent);
            if (!pListItem.LDocketEntryLocked)
            {
                pListDragOrigin = pRowEvent.GetPosition(null);
                pListDragOffset = pRowEvent.GetPosition(pRowBorder);
                pListDragPath = pRowPath;
                pRowBorder.CaptureMouse();
            }
            pRowEvent.Handled = true;
        };
        pRowBorder.MouseMove += (pRowSender, pRowEvent) => PListDragHandle(pRowSender, pRowEvent);
        pRowBorder.MouseLeftButtonUp += (_, _) =>
        {
            pRowBorder.ReleaseMouseCapture();
            pListDragOrigin = null;
            pListDragPath = null;
            PListReleaseHandle();
        };
        pListRows[pRowPath] = pRowBorder;
        return pRowBorder;
    }

    private UIElement PListCardBuild(IReadOnlyList<LDocketEntry> pListLockedItems)
    {
        var pListCardRows = new StackPanel();
        var pListHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 7, 10, 5)
        };
        pListHeader.Children.Add(new TextBlock
        {
            Text = "",
            FontSize = 11,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Foreground = pListMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });
        pListHeader.Children.Add(new TextBlock
        {
            Text = pListLockedItems.Count == 1
                ? LLocalization.LLocalizationTextRead("List.Locked.SummaryOne")
                : LLocalization.LLocalizationFormat("List.Locked.SummaryMany", pListLockedItems.Count),
            FontSize = 11,
            FontFamily = pListFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pListMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        pListCardRows.Children.Add(pListHeader);
        for (int pListIndex = 0; pListIndex < pListLockedItems.Count; pListIndex++)
        {
            pListCardRows.Children.Add(PListRowBuild(
                pListLockedItems[pListIndex], pListIndex < pListLockedItems.Count - 1));
        }

        return new Border
        {
            Margin = new Thickness(6, 6, 6, 0),
            Background = pListLockedBrush,
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(0, 0, 0, 4),
            Child = pListCardRows
        };
    }

    private Brush PListBackgroundRead(LDocketEntry pListItem) =>
        PListSelectionCheck(pListItem.LDocketEntryPath)
            ? pListItem.LDocketEntryLocked ? pListLockedAccent : pListSelectBrush
            : pListItem.LDocketEntryLocked ? Brushes.Transparent : Brushes.White;

    private void PListDragHandle(object pRowSender, MouseEventArgs pRowEvent)
    {
        if (pListDragOrigin is not { } pStart
            || pListDragPath is not { } pDragPath
            || pRowEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrent = pRowEvent.GetPosition(null);
        if (Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        string[] pDragPaths = PListSelectionCheck(pDragPath)
            ? PListSelectionRead()
                .Where(pListPath => !PListLockCheck(pListPath))
                .ToArray()
            : [pDragPath];
        if (pDragPaths.Length == 0)
        {
            return;
        }
        var pDragData = new DataObject(PListDragKind, pDragPaths);
        Point pGrabOffset = pListDragOffset;
        pListDragOrigin = null;
        pListDragPath = null;
        pListPressPath = null;
        if (pRowSender is UIElement pRowElement)
        {
            pRowElement.ReleaseMouseCapture();
        }

        if (pRowSender is FrameworkElement pRowVisual)
        {
            PGhost.PGhostDragRun(
                pRowVisual,
                pGrabOffset,
                () => DragDrop.DoDragDrop(pRowVisual, pDragData, DragDropEffects.Copy));
            return;
        }

        DragDrop.DoDragDrop((DependencyObject)pRowSender, pDragData, DragDropEffects.Copy);
    }

    private void PListEmptyUpdate()
    {
        pListEmptyNotice.Visibility = pListDocket.LDocketItemsRead().Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}

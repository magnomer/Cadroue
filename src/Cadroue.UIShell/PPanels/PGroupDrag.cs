using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PGroup
{
    private const string PGroupMoveKind = "CadroueGroupMove";

    private Point? pGroupDragOrigin;
    private Point pGroupDragOffset;
    private int? pGroupSourceIndex;
    private string? pGroupDragPath;

    private void PGroupDragHandle(object pRowSender, MouseEventArgs pRowEvent)
    {
        if (pGroupDragOrigin is not { } pStart
            || pGroupSourceIndex is not { } pSourceIndex
            || pGroupDragPath is not { } pDragPath
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

        var pData = new DataObject(PGroupMoveKind, new PGroupMovePayload(pSourceIndex, pDragPath));
        Point pGrabOffset = pGroupDragOffset;
        PGroupDragClear();
        if (pRowSender is UIElement pRowElement)
        {
            pRowElement.ReleaseMouseCapture();
        }

        if (pRowSender is FrameworkElement pRowVisual)
        {
            PGhost.PGhostDragRun(
                pRowVisual,
                pGrabOffset,
                () => DragDrop.DoDragDrop(pRowVisual, pData, DragDropEffects.Move));
            return;
        }

        DragDrop.DoDragDrop((DependencyObject)pRowSender, pData, DragDropEffects.Move);
    }

    private void PGroupDragClear()
    {
        pGroupDragOrigin = null;
        pGroupSourceIndex = null;
        pGroupDragPath = null;
    }

    private static void PGroupOverHandle(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = pEvent.Data.GetDataPresent(PGroupMoveKind)
            ? DragDropEffects.Move
            : DragDropEffects.Copy;
        pEvent.Handled = true;
    }

    private static int PGroupInsertResolve(StackPanel pFileRows, DragEventArgs pEvent)
    {
        Point pPoint = pEvent.GetPosition(pFileRows);
        for (int pIndex = 0; pIndex < pFileRows.Children.Count; pIndex++)
        {
            if (pFileRows.Children[pIndex] is not FrameworkElement pRow)
            {
                continue;
            }

            Point pTopLeft = pRow.TranslatePoint(new Point(0, 0), pFileRows);
            if (pPoint.Y < pTopLeft.Y + (pRow.ActualHeight / 2))
            {
                return pIndex;
            }
        }

        return pFileRows.Children.Count;
    }

    private void PGroupCardHandle(int pTargetIndex, StackPanel pFileRows, DragEventArgs pEvent)
    {
        LTraceLog.LTraceInfoRecord($"DRAGTRACE group card drop target={pTargetIndex}");
        int pInsertAt = PGroupInsertResolve(pFileRows, pEvent);
        List<string> pTargetPaths = pGroupRecords[pTargetIndex].PGroupRecordPaths;

        if (pEvent.Data.GetData(PGroupMoveKind) is PGroupMovePayload pMove
            && pMove.PGroupMoveSourceIndex >= 0
            && pMove.PGroupMoveSourceIndex < pGroupRecords.Count)
        {
            List<string> pSourcePaths = pGroupRecords[pMove.PGroupMoveSourceIndex].PGroupRecordPaths;
            int pRemovedIndex = pSourcePaths.FindIndex(pPath =>
                string.Equals(pPath, pMove.PGroupMovePath, StringComparison.OrdinalIgnoreCase));
            if (pRemovedIndex >= 0)
            {
                pSourcePaths.RemoveAt(pRemovedIndex);
                if (pMove.PGroupMoveSourceIndex == pTargetIndex && pRemovedIndex < pInsertAt)
                {
                    pInsertAt--;
                }
            }

            PGroupPathInsert(pTargetPaths, pMove.PGroupMovePath, pInsertAt);
        }
        else if (PGroupPathsRead(pEvent) is { Count: > 0 } pAddPaths)
        {
            foreach (string pAddPath in pAddPaths)
            {
                PGroupPathInsert(pTargetPaths, pAddPath, pInsertAt);
                pInsertAt++;
            }
        }
        else
        {
            return;
        }

        pEvent.Handled = true;
        PGroupRebuild();
    }

    private void PGroupDropHandle(object pSender, DragEventArgs pEvent)
    {
        LTraceLog.LTraceInfoRecord("DRAGTRACE group container drop");
        if (pEvent.Handled)
        {
            return;
        }

        var pNewPaths = new List<string>();
        if (pEvent.Data.GetData(PGroupMoveKind) is PGroupMovePayload pMove
            && pMove.PGroupMoveSourceIndex >= 0
            && pMove.PGroupMoveSourceIndex < pGroupRecords.Count)
        {
            List<string> pSourcePaths = pGroupRecords[pMove.PGroupMoveSourceIndex].PGroupRecordPaths;
            if (pSourcePaths.RemoveAll(pPath =>
                    string.Equals(pPath, pMove.PGroupMovePath, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                pNewPaths.Add(pMove.PGroupMovePath);
            }
        }
        else
        {
            pNewPaths.AddRange(PGroupPathsRead(pEvent));
        }

        if (pNewPaths.Count == 0)
        {
            return;
        }

        var pRecord = new PGroupRecord
        {
            PGroupRecordName = LLocalization.LLocalizationFormat("Group.Default.Name", pGroupRecords.Count + 1)
        };
        foreach (string pPath in pNewPaths)
        {
            PGroupPathInsert(pRecord.PGroupRecordPaths, pPath, pRecord.PGroupRecordPaths.Count);
        }

        pGroupRecords.Add(pRecord);
        pEvent.Handled = true;
        PGroupRebuild();
    }

    private IReadOnlyList<string> PGroupPathsRead(DragEventArgs pEvent)
    {
        if (pEvent.Data.GetData(PList.PListDragKind) is string[] pListPaths)
        {
            return pListPaths;
        }

        if (pEvent.Data.GetData(DataFormats.FileDrop) is string[] pFilePaths)
        {
            return PGroupFileRequest?.Invoke(pFilePaths) ?? PList.PListMediaScan(pFilePaths);
        }

        return Array.Empty<string>();
    }

    private static void PGroupPathInsert(List<string> pPaths, string pPath, int pInsertAt)
    {
        if (pPaths.Any(pExisting => string.Equals(pExisting, pPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        pPaths.Insert(Math.Clamp(pInsertAt, 0, pPaths.Count), pPath);
    }

    private sealed record PGroupMovePayload(int PGroupMoveSourceIndex, string PGroupMovePath);
}

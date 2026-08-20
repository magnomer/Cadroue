using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIShell.PMainWindow;

using Cadroue.Infrastructure;

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
        if (PGroupExternalAccept(pEvent))
        {
            return;
        }

        int pInsertAt = PGroupInsertResolve(pFileRows, pEvent);
        List<string> pTargetPaths = pGroupRecords[pTargetIndex].PGroupRecordPaths;

        if (pEvent.Data.GetData(PGroupMoveKind) is PGroupMovePayload pMove
            && pMove.PGroupMoveIndex >= 0
            && pMove.PGroupMoveIndex < pGroupRecords.Count)
        {
            List<string> pSourcePaths = pGroupRecords[pMove.PGroupMoveIndex].PGroupRecordPaths;
            int pRemovedIndex = pSourcePaths.FindIndex(pPath =>
                string.Equals(pPath, pMove.PGroupMovePath, StringComparison.OrdinalIgnoreCase));
            if (pRemovedIndex >= 0)
            {
                pSourcePaths.RemoveAt(pRemovedIndex);
                if (pMove.PGroupMoveIndex == pTargetIndex && pRemovedIndex < pInsertAt)
                {
                    pInsertAt--;
                }
            }

            PGroupPathInsert(pTargetPaths, pMove.PGroupMovePath, pInsertAt);
            LTraceLog.LTraceInfoRecord(pMove.PGroupMoveIndex == pTargetIndex
                ? $"Group {pTargetIndex + 1}: reordered '{Path.GetFileName(pMove.PGroupMovePath)}'"
                : $"Group {pTargetIndex + 1}: moved in '{Path.GetFileName(pMove.PGroupMovePath)}' from group {pMove.PGroupMoveIndex + 1}");
        }
        else if (PGroupPathsRead(pEvent) is { Count: > 0 } pAddPaths)
        {
            foreach (string pAddPath in pAddPaths)
            {
                PGroupPathInsert(pTargetPaths, pAddPath, pInsertAt);
                pInsertAt++;
            }

            LTraceLog.LTraceInfoRecord($"Group {pTargetIndex + 1}: added {pAddPaths.Count} file(s)");
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
        if (pEvent.Handled)
        {
            return;
        }

        if (PGroupExternalAccept(pEvent))
        {
            return;
        }

        var pNewPaths = new List<string>();
        if (pEvent.Data.GetData(PGroupMoveKind) is PGroupMovePayload pMove
            && pMove.PGroupMoveIndex >= 0
            && pMove.PGroupMoveIndex < pGroupRecords.Count)
        {
            List<string> pSourcePaths = pGroupRecords[pMove.PGroupMoveIndex].PGroupRecordPaths;
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
        LTraceLog.LTraceInfoRecord($"Group {pGroupRecords.Count}: created with {pNewPaths.Count} file(s)");
        pEvent.Handled = true;
        PGroupRebuild();
    }

    private bool PGroupExternalAccept(DragEventArgs pEvent)
    {
        if (pEvent.Data.GetData(DataFormats.FileDrop) is not string[] pFilePaths)
        {
            return false;
        }

        PGroupFileRequest?.Invoke(pFilePaths);
        pEvent.Handled = true;
        return true;
    }

    private static IReadOnlyList<string> PGroupPathsRead(DragEventArgs pEvent) =>
        pEvent.Data.GetData(PList.PListDragKind) is string[] pListPaths
            ? pListPaths
            : Array.Empty<string>();

    private static void PGroupPathInsert(List<string> pPaths, string pPath, int pInsertAt)
    {
        if (pPaths.Any(pExisting => string.Equals(pExisting, pPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        pPaths.Insert(Math.Clamp(pInsertAt, 0, pPaths.Count), pPath);
    }

    private sealed record PGroupMovePayload(int PGroupMoveIndex, string PGroupMovePath);
}

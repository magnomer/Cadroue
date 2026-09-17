using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PGroup
{
    private const string PGroupMoveKind = "CadroueGroupMove";

    private Point? pGroupDragOrigin;
    private Point pGroupDragOffset;

    private void PGroupDragHandle(object pRowSender, MouseEventArgs pRowEvent)
    {
        if (pGroupDragOrigin is not { } pStart
            || LGroup.LGroupSourceIndex is not { } pSourceIndex
            || LGroup.LGroupDragPath is not { } pDragPath
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
        LGroup.LGroupDragSet(null, null);
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
        bool pChanged = pEvent.Data.GetData(PGroupMoveKind) is PGroupMovePayload pMove
            ? LGroup.LGroupItemMove(pMove.PGroupMoveIndex, pMove.PGroupMovePath, pTargetIndex, pInsertAt)
            : LGroup.LGroupPathsInsert(pTargetIndex, PGroupPathsRead(pEvent), pInsertAt);
        if (pChanged)
        {
            pEvent.Handled = true;
        }
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

        string pName = LLocalization.LLocalizationFormat("Group.Default.Name", LGroup.LGroupRecords.Count + 1);
        bool pChanged = pEvent.Data.GetData(PGroupMoveKind) is PGroupMovePayload pMove
            ? LGroup.LGroupAdd([pMove.PGroupMovePath], pName, pMove.PGroupMoveIndex)
            : LGroup.LGroupAdd(PGroupPathsRead(pEvent), pName);
        if (pChanged)
        {
            pEvent.Handled = true;
        }
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

    private sealed record PGroupMovePayload(int PGroupMoveIndex, string PGroupMovePath);
}

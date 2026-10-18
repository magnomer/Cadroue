using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PGroupCardDrag
{
    private static readonly IReadOnlyDictionary<bool, Action<PGroupCardDrag, Border>> pGroupDrags =
        new Dictionary<bool, Action<PGroupCardDrag, Border>>
        {
            [true] = (pDrag, pRow) => pDrag.PGroupDragRun(pRow),
            [false] = (pDrag, pRow) => { },
        };

    private readonly QGroupDrag lGroupDrag;

    public PGroupCardDrag(QGroupDrag lDrag) => lGroupDrag = lDrag;

    public void PGroupRowAttach(QGroupFile lFile, Border pRowBorder)
    {
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) => PGroupPressHandle(lFile, pRowBorder, pRowEvent);
        pRowBorder.MouseMove += (_, pRowEvent) => PGroupMoveHandle(pRowBorder, pRowEvent);
        pRowBorder.MouseLeftButtonUp += (_, _) => PGroupReleaseHandle(pRowBorder);
    }

    private void PGroupReleaseHandle(Border pRowBorder)
    {
        pRowBorder.ReleaseMouseCapture();
        lGroupDrag.QGroupDragClear();
    }

    private void PGroupPressHandle(QGroupFile lFile, Border pRowBorder, MouseButtonEventArgs pRowEvent)
    {
        Point pOrigin = pRowEvent.GetPosition(null);
        Point pGrab = pRowEvent.GetPosition(pRowBorder);
        lGroupDrag.QGroupPressHandle(
            lFile.QGroupFileGroup, lFile.QGroupFilePath, pOrigin.X, pOrigin.Y, pGrab.X, pGrab.Y);
        pRowBorder.CaptureMouse();
    }

    private void PGroupMoveHandle(Border pRowBorder, MouseEventArgs pRowEvent)
    {
        Point pCurrent = pRowEvent.GetPosition(null);
        bool pStart = lGroupDrag.QGroupDragResolve(
            pCurrent.X,
            pCurrent.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            PLook.PLookPressed[pRowEvent.LeftButton]);
        pGroupDrags[pStart](this, pRowBorder);
    }

    private void PGroupDragRun(Border pRowBorder)
    {
        DataObject pData = PGroupPayloadCreate();
        QGhost lGhost = lGroupDrag.QGroupGhost;
        lGroupDrag.QGroupDragClear();
        pRowBorder.ReleaseMouseCapture();
        QGhostLayer.QGhostLayerRun(
            pRowBorder,
            lGhost,
            () => DragDrop.DoDragDrop(pRowBorder, pData, DragDropEffects.Move));
    }

    private DataObject PGroupPayloadCreate() =>
        new(
            PGroupDrop.PGroupMoveKind,
            new PGroupDrop.PGroupMovePayload(lGroupDrag.QGroupDragIndex, lGroupDrag.QGroupDragPath));
}

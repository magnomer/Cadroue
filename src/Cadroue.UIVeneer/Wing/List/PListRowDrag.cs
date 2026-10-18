using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PListRowDrag
{
    private static readonly IReadOnlyDictionary<bool, Func<Border, IInputElement?>> pListCaptures =
        new Dictionary<bool, Func<Border, IInputElement?>>
        {
            [true] = pBorder => pBorder,
            [false] = pBorder => null,
        };

    private static readonly IReadOnlyDictionary<bool, Action<PListRowDrag>> pListDrags =
        new Dictionary<bool, Action<PListRowDrag>>
        {
            [true] = pDrag => pDrag.PListDragRun(),
            [false] = pDrag => { },
        };

    private readonly UIElement pListFocus;
    private readonly QList lList;
    private readonly string pListRowPath;
    private readonly Border pListRowBorder;

    public PListRowDrag(UIElement pFocus, QList lListOwner, string pRowPath, Border pRowBorder)
    {
        pListFocus = pFocus;
        lList = lListOwner;
        pListRowPath = pRowPath;
        pListRowBorder = pRowBorder;
    }

    public void PListRowAttach()
    {
        pListRowBorder.MouseLeftButtonDown += PListPressHandle;
        pListRowBorder.MouseMove += PListMoveHandle;
        pListRowBorder.MouseLeftButtonUp += PListReleaseHandle;
    }

    private void PListPressHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        pListFocus.Focus();
        Point pOrigin = pEvent.GetPosition(null);
        Point pGrab = pEvent.GetPosition(pListRowBorder);
        bool pCapture = lList.QListDrag.QListPressHandle(
            pListRowPath,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            pOrigin.X,
            pOrigin.Y,
            pGrab.X,
            pGrab.Y);
        Mouse.Capture(pListCaptures[pCapture](pListRowBorder));
        pEvent.Handled = true;
    }

    private void PListMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        Point pCurrent = pEvent.GetPosition(null);
        bool pStart = lList.QListDrag.QListDragResolve(
            pCurrent.X,
            pCurrent.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            PLook.PLookPressed[pEvent.LeftButton]);
        pListDrags[pStart](this);
    }

    private void PListDragRun()
    {
        var pDragData = new DataObject(PList.PListDragKind, lList.QListDrag.QListDragPaths.ToArray());
        pListRowBorder.ReleaseMouseCapture();
        QGhostLayer.QGhostLayerRun(
            pListRowBorder,
            lList.QListDrag.QListGhost,
            () => DragDrop.DoDragDrop(pListRowBorder, pDragData, DragDropEffects.Copy));
    }

    private void PListReleaseHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        pListRowBorder.ReleaseMouseCapture();
        lList.QListDrag.QListReleaseHandle();
    }
}

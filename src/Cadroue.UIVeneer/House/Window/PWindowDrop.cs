using System.Windows;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PWindowDrop(QWindowDrop lDrop)
{
    private static readonly Type[] PDropGroupTypes = [typeof(PGroup)];

    public void PDropHandlersAdd(UIElement pTarget)
    {
        pTarget.AddHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropEnterHandle), true);
        pTarget.AddHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropOverHandle), true);
        pTarget.AddHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle), true);
    }

    public void PDropHandlersRemove(UIElement pTarget)
    {
        pTarget.RemoveHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropEnterHandle));
        pTarget.RemoveHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropOverHandle));
        pTarget.RemoveHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle));
    }

    private void PDropEnterHandle(object sender, DragEventArgs dragEvent)
    {
        lDrop.QWindowEnterHandle(PDropDragRead(dragEvent));
        PDropResultApply(dragEvent);
    }

    private void PDropOverHandle(object sender, DragEventArgs dragEvent)
    {
        lDrop.QWindowOverHandle(PDropDragRead(dragEvent));
        PDropResultApply(dragEvent);
    }

    private void PDropHandle(object sender, DragEventArgs dragEvent)
    {
        lDrop.QWindowDropHandle(PDropDragRead(dragEvent));
        PDropResultApply(dragEvent);
    }

    private void PDropResultApply(DragEventArgs dragEvent)
    {
        dragEvent.Effects = PLook.PLookDropEffect[lDrop.QWindowDropEffect];
        dragEvent.Handled = lDrop.QWindowDropHandled;
    }

    private static QWindowDrag PDropDragRead(DragEventArgs dragEvent) => new(
        dragEvent.Data.GetData(DataFormats.FileDrop) as string[],
        QWalk.QWalkParentCheck(dragEvent.OriginalSource as DependencyObject, PDropGroupMatch),
        dragEvent.OriginalSource?.GetType().Name,
        dragEvent.AllowedEffects.HasFlag(DragDropEffects.Copy),
        dragEvent.Data);

    private static bool PDropGroupMatch(DependencyObject pNode) => QWalkType.QWalkTypeMatch(pNode, PDropGroupTypes);
}

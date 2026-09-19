using System.Windows;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PHouse;

public partial class PWindow
{
    private static readonly Type[] PDropGroupTypes = [typeof(PGroup)];

    private void PDropHandlersAdd()
    {
        AddHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropEnterHandle), true);
        AddHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropOverHandle), true);
        AddHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle), true);
    }

    private void PDropHandlersRemove()
    {
        RemoveHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropEnterHandle));
        RemoveHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropOverHandle));
        RemoveHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle));
    }

    private void PDropEnterHandle(object sender, DragEventArgs dragEvent)
    {
        lWindow.LWindowDrop.LWindowEnterHandle(PDropDragRead(dragEvent));
        PDropResultApply(dragEvent);
    }

    private void PDropOverHandle(object sender, DragEventArgs dragEvent)
    {
        lWindow.LWindowDrop.LWindowOverHandle(PDropDragRead(dragEvent));
        PDropResultApply(dragEvent);
    }

    private void PDropHandle(object sender, DragEventArgs dragEvent)
    {
        lWindow.LWindowDrop.LWindowDropHandle(PDropDragRead(dragEvent));
        PDropResultApply(dragEvent);
    }

    private void PDropResultApply(DragEventArgs dragEvent)
    {
        dragEvent.Effects = PLook.PLookDropEffect[lWindow.LWindowDrop.LWindowDropEffect];
        dragEvent.Handled = lWindow.LWindowDrop.LWindowDropHandled;
    }

    private static LWindowDrag PDropDragRead(DragEventArgs dragEvent) => new(
        dragEvent.Data.GetData(DataFormats.FileDrop) as string[],
        PWalk.PWalkParentCheck(dragEvent.OriginalSource as DependencyObject, PDropGroupMatch),
        dragEvent.OriginalSource?.GetType().Name,
        dragEvent.AllowedEffects.HasFlag(DragDropEffects.Copy),
        dragEvent.Data);

    private static bool PDropGroupMatch(DependencyObject pNode) => PWalk.PWalkTypeCheck(pNode, PDropGroupTypes);
}

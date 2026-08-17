using System.IO;
using System.Windows;
using System.Windows.Media;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    private DragDropEffects? pDropLastEffect;
    private void PDropHandlersAdd()
    {
        AddHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropEnterHandle), true);
        AddHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropAccept), true);
        AddHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle), true);
    }

    private void PDropHandlersRemove()
    {
        RemoveHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropEnterHandle));
        RemoveHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropAccept));
        RemoveHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle));
    }

    private void PDropEnterHandle(object sender, DragEventArgs dragEvent)
    {
        bool pDropGroup = PDropGroupCheck(dragEvent);
        DragDropEffects pDropEffect = pDropGroup ? DragDropEffects.None : PDropEffectRead(dragEvent, out _);
        LTraceLog.LTraceInfoRecord(
            $"Drag entered window: {(pDropGroup ? "handed to PGroup (window ignores)" : pDropEffect == DragDropEffects.None ? "will REFUSE (forbidden cursor)" : $"will accept ({pDropEffect})")}",
            $"originalSource={dragEvent.OriginalSource?.GetType().Name ?? "null"}, "
            + $"list={(pListActive is null ? "NULL" : "present")}, viewer={(pViewerActive is null ? "NULL" : "present")}, "
            + $"audioTab={pWindowAudioAllowed}, groupAncestor={pDropGroup}");

        PDropAccept(sender, dragEvent);
    }

    private void PDropAccept(object sender, DragEventArgs dragEvent)
    {
        if (PDropGroupCheck(dragEvent))
        {
            pDropLastEffect = null;
            return;
        }

        DragDropEffects dropEffect = PDropEffectRead(dragEvent, out string dropReason);
        if (dropEffect != pDropLastEffect)
        {
            pDropLastEffect = dropEffect;
            LTraceLog.LTraceInfoRecord(
                $"Drag over: {(dropEffect == DragDropEffects.None ? "REFUSED (forbidden cursor)" : dropEffect.ToString())}",
                dropReason);
        }

        dragEvent.Effects = dropEffect;
        dragEvent.Handled = true;
    }

    private void PDropHandle(object sender, DragEventArgs dragEvent)
    {
        if (PDropGroupCheck(dragEvent))
        {
            return;
        }

        pDropLastEffect = null;
        DragDropEffects dropEffect = PDropEffectRead(dragEvent, out string dropReason);
        dragEvent.Effects = dropEffect;
        dragEvent.Handled = true;

        string dropTarget = pListActive is not null ? "list" : pViewerActive is not null ? "viewer" : "none";
        LTraceLog.LTraceInfoRecord(
            $"Drop released: {(dropEffect == DragDropEffects.None ? "REFUSED" : "accepted")} onto {dropTarget}",
            dropReason);

        if (dropEffect == DragDropEffects.None)
        {
            return;
        }

        if (pListActive is not null)
        {
            IReadOnlyList<string> dropPaths = PDropPathsRead(dragEvent);
            int dropAdded = pListActive.PListPathsAdd(dropPaths);
            LTraceLog.LTraceInfoRecord($"Drop into list: {dropAdded} of {dropPaths.Count} path(s) added");
            return;
        }

        if (pViewerActive is null)
        {
            return;
        }

        string? sourcePath = PDropPathRead(dragEvent);
        if (sourcePath is null)
        {
            dragEvent.Effects = DragDropEffects.None;
            LTraceLog.LTraceWarningRecord("Drop into viewer refused: no existing file in payload");
            return;
        }

        pViewerActive.PViewerSourceOpen(sourcePath);
    }

    private static bool PDropGroupCheck(DragEventArgs dragEvent)
    {
        DependencyObject? pNode = dragEvent.OriginalSource as DependencyObject;
        while (pNode is not null)
        {
            if (pNode is PGroup)
            {
                return true;
            }

            pNode = pNode is Visual pVisual ? VisualTreeHelper.GetParent(pVisual) : LogicalTreeHelper.GetParent(pNode);
        }

        return false;
    }

    private DragDropEffects PDropEffectRead(DragEventArgs dragEvent, out string pDropReason)
    {
        IReadOnlyList<string> pDropPaths = PDropPathsRead(dragEvent);
        string pDropPayload = pDropPaths.Count == 0
            ? "no FileDrop payload"
            : $"{pDropPaths.Count} path(s): {string.Join(", ", pDropPaths.Select(System.IO.Path.GetFileName))}";

        if (pListActive is not null)
        {
            bool pDropListMatch = pDropPaths
                .Any(pDropPath => Directory.Exists(pDropPath) || PList.PListMediaCheck(pDropPath));
            pDropReason = pDropListMatch
                ? $"target=list, {pDropPayload}"
                : $"target=list, none are media/folders — {pDropPayload}";
            return pDropListMatch ? PDropAllowedRead(dragEvent) : DragDropEffects.None;
        }

        if (pViewerActive is null)
        {
            pDropReason = $"no active list or viewer (active tab has no drop target); {pDropPayload}";
            return DragDropEffects.None;
        }

        string? pSourcePath = PDropPathRead(dragEvent);
        if (pSourcePath is null)
        {
            pDropReason = $"target=viewer, no existing file in payload — {pDropPayload}";
            return DragDropEffects.None;
        }

        if (Cadroue.Media.LMedia.LMediaAudioCheck(pSourcePath) && !pWindowAudioAllowed)
        {
            pDropReason = $"target=viewer, audio-only file on a video-only tab — {System.IO.Path.GetFileName(pSourcePath)}";
            return DragDropEffects.None;
        }

        pDropReason = $"target=viewer, {System.IO.Path.GetFileName(pSourcePath)}";
        return PDropAllowedRead(dragEvent);
    }

    private static DragDropEffects PDropAllowedRead(DragEventArgs dragEvent)
    {
        if ((dragEvent.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
        {
            return DragDropEffects.Copy;
        }

        if ((dragEvent.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move)
        {
            return DragDropEffects.Move;
        }

        if ((dragEvent.AllowedEffects & DragDropEffects.Link) == DragDropEffects.Link)
        {
            return DragDropEffects.Link;
        }

        return DragDropEffects.None;
    }

    private static IReadOnlyList<string> PDropPathsRead(DragEventArgs dragEvent)
    {
        if (!dragEvent.Data.GetDataPresent(DataFormats.FileDrop)
            || dragEvent.Data.GetData(DataFormats.FileDrop) is not string[] dropPaths)
        {
            return [];
        }

        return dropPaths;
    }

    private static string? PDropPathRead(DragEventArgs dragEvent)
    {
        if (!dragEvent.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return null;
        }

        if (dragEvent.Data.GetData(DataFormats.FileDrop) is not string[] sourcePaths)
        {
            return null;
        }

        foreach (string sourcePath in sourcePaths)
        {
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }

        return null;
    }
}

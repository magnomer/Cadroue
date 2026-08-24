using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    private DragDropEffects? pDropLastEffect;
    private object? pDropTraceData;
    private readonly List<string> pDropTrace = [];
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
        if (!pDropGroup)
        {
            PDropTraceAppend(
                dragEvent,
                $"Drag entered window: {(pDropEffect == DragDropEffects.None ? "will REFUSE (forbidden cursor)" : $"will accept ({pDropEffect})")}",
                $"originalSource={dragEvent.OriginalSource?.GetType().Name ?? "null"}, "
                + $"list={(pListActive is null ? "NULL" : "present")}, viewer={(pViewerActive is null ? "NULL" : "present")}, "
                + $"audioTab={pWindowAudioAllowed}, groupAncestor={pDropGroup}");
        }

        PDropAccept(sender, dragEvent);
    }

    private void PDropAccept(object sender, DragEventArgs dragEvent)
    {
        if (PDropGroupCheck(dragEvent))
        {
            pDropLastEffect = null;
            return;
        }

        PDropTraceStart(dragEvent);
        DragDropEffects dropEffect = PDropEffectRead(dragEvent, out string dropReason);
        if (dropEffect != pDropLastEffect)
        {
            pDropLastEffect = dropEffect;
            PDropTraceAppend(
                dragEvent,
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
        PDropTraceAppend(
            dragEvent,
            $"Drop released: {(dropEffect == DragDropEffects.None ? "REFUSED" : "accepted")} onto {dropTarget}",
            dropReason);

        if (dropEffect == DragDropEffects.None)
        {
            PDropTraceRecord($"File drag refused onto {dropTarget}");
            return;
        }

        if (pListActive is not null)
        {
            IReadOnlyList<string> dropPaths = PDropPathsRead(dragEvent);
            int dropAdded = pListActive.PListPathsAdd(dropPaths);
            PDropTraceAppend(
                dragEvent,
                $"Drop into list: {dropAdded} of {dropPaths.Count} path(s) added");
            PDropTraceRecord($"File drag accepted onto list ({dropEffect})");
            return;
        }

        if (pViewerActive is null)
        {
            PDropTraceRecord($"File drag accepted onto {dropTarget} ({dropEffect})");
            return;
        }

        string? sourcePath = PDropPathRead(dragEvent);
        if (sourcePath is null)
        {
            dragEvent.Effects = DragDropEffects.None;
            PDropTraceAppend(dragEvent, "Drop into viewer refused: no existing file in payload");
            PDropTraceRecord("File drag refused onto viewer", warning: true);
            return;
        }

        pViewerActive.PViewerSourceOpen(sourcePath);
        PDropTraceAppend(dragEvent, $"Drop into viewer: opened {System.IO.Path.GetFileName(sourcePath)}");
        PDropTraceRecord($"File drag accepted onto viewer ({dropEffect})");
    }

    private void PDropTraceAppend(DragEventArgs dragEvent, string pDropSummary, string? pDropDetail = null)
    {
        PDropTraceStart(dragEvent);

        string pDropTime = DateTimeOffset.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        pDropTrace.Add($"{pDropTime}  {pDropSummary}");
        if (!string.IsNullOrWhiteSpace(pDropDetail))
        {
            pDropTrace.Add($"{new string(' ', 14)}{pDropDetail}");
        }
    }

    private void PDropTraceStart(DragEventArgs dragEvent)
    {
        if (!ReferenceEquals(pDropTraceData, dragEvent.Data))
        {
            pDropTraceData = dragEvent.Data;
            pDropTrace.Clear();
            pDropLastEffect = null;
        }
    }

    private void PDropTraceRecord(string pDropSummary, bool warning = false)
    {
        string? pDropDetail = pDropTrace.Count == 0
            ? null
            : string.Join(Environment.NewLine, pDropTrace);
        if (warning)
        {
            LTraceLog.LTraceWarningRecord(pDropSummary, pDropDetail);
        }
        else
        {
            LTraceLog.LTraceInfoRecord(pDropSummary, pDropDetail);
        }

        pDropTraceData = null;
        pDropTrace.Clear();
        pDropLastEffect = null;
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

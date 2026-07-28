using System.IO;
using System.Windows;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    private void PDropHandlersAdd()
    {
        AddHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropAccept), true);
        AddHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropAccept), true);
        AddHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle), true);
    }

    private void PDropHandlersRemove()
    {
        RemoveHandler(DragDrop.PreviewDragEnterEvent, new DragEventHandler(PDropAccept));
        RemoveHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(PDropAccept));
        RemoveHandler(DragDrop.PreviewDropEvent, new DragEventHandler(PDropHandle));
    }

    private void PDropAccept(object sender, DragEventArgs dragEvent)
    {
        dragEvent.Effects = PDropEffectRead(dragEvent);
        dragEvent.Handled = true;
    }

    private void PDropHandle(object sender, DragEventArgs dragEvent)
    {
        DragDropEffects dropEffect = PDropEffectRead(dragEvent);
        dragEvent.Effects = dropEffect;
        dragEvent.Handled = true;
        if (dropEffect == DragDropEffects.None)
        {
            return;
        }

        if (pListActive is not null)
        {
            pListActive.PListPathsAdd(PDropPathsRead(dragEvent));
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
            return;
        }

        pViewerActive.PViewerSourceOpen(sourcePath);
    }

    private DragDropEffects PDropEffectRead(DragEventArgs dragEvent)
    {
        if (pListActive is not null)
        {
            return PDropPathsRead(dragEvent)
                .Any(pDropPath => Directory.Exists(pDropPath) || PList.PListMediaCheck(pDropPath))
                ? PDropAllowedRead(dragEvent)
                : DragDropEffects.None;
        }

        if (pViewerActive is null)
        {
            return DragDropEffects.None;
        }

        string? pSourcePath = PDropPathRead(dragEvent);
        if (pSourcePath is null || PDropAudioCheck(pSourcePath) && !pWindowAudioAllowed)
        {
            return DragDropEffects.None;
        }

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

    private static bool PDropAudioCheck(string pSourcePath)
    {
        string pExtension = Path.GetExtension(pSourcePath);
        return pExtension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".aac", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".flac", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
            || pExtension.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
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

using System.IO;
using System.Windows;

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
        if (dropEffect == DragDropEffects.None || pViewerActive is null)
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
        if (pViewerActive is null)
        {
            return DragDropEffects.None;
        }

        string? pSourcePath = PDropPathRead(dragEvent);
        if (pSourcePath is null || PDropAudioCheck(pSourcePath) && !pWindowAudioAllowed)
        {
            return DragDropEffects.None;
        }

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

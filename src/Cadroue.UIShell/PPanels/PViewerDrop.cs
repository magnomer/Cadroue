using System.IO;
using System.Windows;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private void PDropHandlersAdd()
    {
        pViewerOverlay.DragEnter += PViewerDragAccept;
        pViewerOverlay.DragOver += PViewerDragAccept;
        pViewerOverlay.Drop += PDropHandle;
    }

    private void PDropHandlersRemove()
    {
        pViewerOverlay.DragEnter -= PViewerDragAccept;
        pViewerOverlay.DragOver -= PViewerDragAccept;
        pViewerOverlay.Drop -= PDropHandle;
    }

    private void PViewerDragAccept(object sender, DragEventArgs dragEvent)
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

        string? sourcePath = PDropPathRead(dragEvent);
        if (sourcePath is null)
        {
            dragEvent.Effects = DragDropEffects.None;
            return;
        }

        PViewerSourceOpen(sourcePath);
    }

    private DragDropEffects PDropEffectRead(DragEventArgs dragEvent)
    {
        string? pSourcePath = PDropPathRead(dragEvent);
        if (pSourcePath is null || PDropAudioCheck(pSourcePath) && !pViewerAudioOnlyAllowed)
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

    private static bool PDropAudioCheck(string sourcePath)
    {
        string extension = Path.GetExtension(sourcePath);
        return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".aac", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".flac", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
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

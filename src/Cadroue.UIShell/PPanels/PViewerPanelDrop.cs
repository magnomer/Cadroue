using System.IO;
using System.Windows;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewerPanel
{
    private void PViewerPanelDropHandlersAdd()
    {
        pViewerPanelOverlay.DragEnter += PViewerPanelDragAccept;
        pViewerPanelOverlay.DragOver += PViewerPanelDragAccept;
        pViewerPanelOverlay.Drop += PViewerPanelDrop;
    }

    private void PViewerPanelDropHandlersRemove()
    {
        pViewerPanelOverlay.DragEnter -= PViewerPanelDragAccept;
        pViewerPanelOverlay.DragOver -= PViewerPanelDragAccept;
        pViewerPanelOverlay.Drop -= PViewerPanelDrop;
    }

    private void PViewerPanelDragAccept(object sender, DragEventArgs dragEvent)
    {
        dragEvent.Effects = PViewerPanelDropEffectRead(dragEvent);
        dragEvent.Handled = true;
    }

    private void PViewerPanelDrop(object sender, DragEventArgs dragEvent)
    {
        DragDropEffects dropEffect = PViewerPanelDropEffectRead(dragEvent);
        dragEvent.Effects = dropEffect;
        dragEvent.Handled = true;
        if (dropEffect == DragDropEffects.None)
        {
            return;
        }

        string? sourcePath = PViewerPanelDropSourcePathRead(dragEvent);
        if (sourcePath is null)
        {
            dragEvent.Effects = DragDropEffects.None;
            return;
        }

        PViewerPanelSourceOpenRequest(sourcePath);
    }

    private DragDropEffects PViewerPanelDropEffectRead(DragEventArgs dragEvent)
    {
        string? pSourcePath = PViewerPanelDropSourcePathRead(dragEvent);
        if (pSourcePath is null || PViewerPanelSourcePathAudioExtensionCheck(pSourcePath) && !pViewerPanelAudioOnlyAllowed)
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

    private static bool PViewerPanelSourcePathAudioExtensionCheck(string sourcePath)
    {
        string extension = Path.GetExtension(sourcePath);
        return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".aac", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".flac", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase);
    }

    private static string? PViewerPanelDropSourcePathRead(DragEventArgs dragEvent)
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

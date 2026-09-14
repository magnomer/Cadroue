using System.IO;
using System.Windows;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    public event Action<IReadOnlyList<string>>? PDropPathsChange;

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

        if (PDropPathsChange is not null)
        {
            PDropPathsChange.Invoke(PDropPathsRead(dragEvent));
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

    private static IReadOnlyList<string> PDropPathsRead(DragEventArgs dragEvent)
    {
        if (!dragEvent.Data.GetDataPresent(DataFormats.FileDrop)
            || dragEvent.Data.GetData(DataFormats.FileDrop) is not string[] dropPaths)
        {
            return [];
        }

        return dropPaths;
    }

    private DragDropEffects PDropEffectRead(DragEventArgs dragEvent)
    {
        if (PDropPathsChange is not null)
        {
            if (!PDropPathsRead(dragEvent)
                .Any(pDropPath => Directory.Exists(pDropPath) || PList.PListMediaCheck(pDropPath)))
            {
                return DragDropEffects.None;
            }

            return PHouse.PWindow.PDropAllowedRead(dragEvent);
        }

        string? pSourcePath = PDropPathRead(dragEvent);
        if (pSourcePath is null || Cadroue.Media.LMedia.LMediaAudioCheck(pSourcePath) && !pViewerAudioAllowed)
        {
            return DragDropEffects.None;
        }

        return PHouse.PWindow.PDropAllowedRead(dragEvent);
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

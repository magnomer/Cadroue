using System.Windows;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    public void PViewerClose()
    {
        if (pViewerUnloaded)
        {
            return;
        }

        try
        {
            pViewerUnloaded = true;
            pViewerLoadSerial++;
            pViewerClockTimer.Tick -= PViewerClockHandle;
            pViewerOverlay.MouseLeftButtonDown -= PCropPressHandle;
            pViewerOverlay.MouseMove -= PCropMoveHandle;
            pViewerOverlay.MouseLeftButtonUp -= PCropReleaseHandle;
            pViewerOverlay.SizeChanged -= PCropSizeHandle;
            PDropHandlersRemove();
            PPlayerStopDispose();
            PViewerFlyleafDispose();
        }
        catch
        {
        }
    }

    public bool PViewerMediaClose()
    {
        if (pViewerUnloaded || !pViewerCommandActive)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(PViewerSourcePath) && pViewerMediaInfo is null)
        {
            return false;
        }

        string pViewerClosedPath = PViewerSourcePath ?? string.Empty;
        pViewerLoadSerial++;
        PPlayerStopDispose();
        PViewerSourcePath = null;
        pViewerMediaInfo = null;
        PCropVideo = null;
        LPreviewStateCurrent = LPreviewStateCurrent
            .LCropboxChange(null)
            .LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        PCropHide();
        PViewerPreviewApply();

        LAppLog.LInfo(string.IsNullOrWhiteSpace(pViewerClosedPath)
            ? "Media closed"
            : $"Media closed '{System.IO.Path.GetFileName(pViewerClosedPath)}' [{pViewerClosedPath}]");

        PViewerMediaRaise(new LMediaOpenStatus(string.Empty, null, false, false, null, null));
        return true;
    }

    private void PViewerFlyleafDispose()
    {
        try
        {
            pViewerFlyleafHost.Player = null;
            pViewerFlyleafHost.Content = null;
            pViewerSurface.Child = null;
            ((IDisposable)pViewerFlyleafHost).Dispose();
        }
        catch
        {
        }
    }
}

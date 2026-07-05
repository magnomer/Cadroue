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

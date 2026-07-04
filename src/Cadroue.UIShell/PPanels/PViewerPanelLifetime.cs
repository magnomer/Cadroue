using System.Windows;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewerPanel
{
    public void PViewerPanelCloseRequest()
    {
        if (pViewerPanelUnloaded)
        {
            return;
        }

        try
        {
            pViewerPanelUnloaded = true;
            pViewerPanelLoadSerial++;
            pViewerPanelClockTimer.Tick -= PViewerPanelClockTickHandle;
            pViewerPanelOverlay.MouseLeftButtonDown -= PViewerPanelCropMouseDown;
            pViewerPanelOverlay.MouseMove -= PViewerPanelCropMouseMove;
            pViewerPanelOverlay.MouseLeftButtonUp -= PViewerPanelCropMouseUp;
            pViewerPanelOverlay.SizeChanged -= PViewerPanelOverlaySizeChanged;
            PViewerPanelDropHandlersRemove();
            PViewerPanelPlayerStopDispose();
            PViewerPanelFlyleafHostDispose();
        }
        catch
        {
        }
    }

    private void PViewerPanelFlyleafHostDispose()
    {
        try
        {
            pViewerPanelFlyleafHost.Player = null;
            pViewerPanelFlyleafHost.Content = null;
            pViewerPanelSurface.Child = null;
            ((IDisposable)pViewerPanelFlyleafHost).Dispose();
        }
        catch
        {
        }
    }
}

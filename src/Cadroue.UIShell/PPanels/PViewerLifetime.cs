using System.Windows;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private int pViewerHostStamp;

    private void PViewerHostWatch()
    {
        Loaded += (_, _) => PViewerHostRecord("panel loaded");
        IsVisibleChanged += (_, pVisibleEvent) =>
            PViewerHostRecord($"panel visible {pVisibleEvent.NewValue}");
        pViewerFlyleafHost.Loaded += (_, _) => PViewerHostRecord("host loaded");
        pViewerFlyleafHost.IsVisibleChanged += (_, pVisibleEvent) =>
            PViewerHostRecord($"host visible {pVisibleEvent.NewValue}");
        pViewerFlyleafHost.SizeChanged += (_, pSizeEvent) =>
            PViewerHostRecord($"host sized {pSizeEvent.NewSize.Width:0}x{pSizeEvent.NewSize.Height:0}");
        PViewerHostRecord("host built");
    }

    private void PViewerHostShow(bool pViewerHostVisible)
    {
        Visibility pViewerHostTarget = pViewerHostVisible ? Visibility.Visible : Visibility.Collapsed;
        if (pViewerFlyleafHost.Visibility == pViewerHostTarget)
        {
            return;
        }

        pViewerFlyleafHost.Visibility = pViewerHostTarget;
        PViewerHostRecord($"host {(pViewerHostVisible ? "shown" : "hidden")}");
    }

    private void PViewerHostRecord(string pViewerStage)
    {
        Window? pViewerHostSurface = null;
        IntPtr pViewerHostHandle = IntPtr.Zero;
        bool pViewerHostDisposed = false;
        try
        {
            pViewerHostDisposed = pViewerFlyleafHost.Disposed;
            pViewerHostSurface = pViewerFlyleafHost.Surface;
            pViewerHostHandle = pViewerFlyleafHost.SurfaceHandle;
        }
        catch
        {
        }

        LAppLog.LInfo(
            $"Viewer host [{++pViewerHostStamp}] {pViewerStage}: "
            + $"panel visible {IsVisible}, host visible {pViewerFlyleafHost.IsVisible}, "
            + $"surface {(pViewerHostSurface is null ? "none" : $"{pViewerHostSurface.Visibility} {pViewerHostSurface.Width:0}x{pViewerHostSurface.Height:0}")}, "
            + $"handle {(pViewerHostHandle == IntPtr.Zero ? "none" : "set")}, "
            + $"player {(pViewerPlayer is null ? "none" : "ready")}, "
            + $"renderer {(pViewerPlayer?.Renderer is null ? "none" : "ready")}, "
            + $"command {(pViewerCommandActive ? "on" : "off")}, "
            + $"disposed {pViewerHostDisposed}");
    }

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
        PViewerHostShow(false);
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

using System.Windows;
using System.Windows.Input;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Infrastructure;
using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private int pViewerHostStamp;

    private void PViewerHostAttach()
    {
        Loaded += (_, _) => PViewerHostRecord("panel loaded");
        IsVisibleChanged += (_, pVisibleEvent) =>
            PViewerHostRecord($"panel visible {pVisibleEvent.NewValue}");
        pViewerFlyleafHost.Loaded += (_, _) =>
        {
            PViewerHostRecord("host loaded");
            PViewerKeyForwardAttach();
        };
        pViewerFlyleafHost.IsVisibleChanged += (_, pVisibleEvent) =>
            PViewerHostRecord($"host visible {pVisibleEvent.NewValue}");
        pViewerFlyleafHost.SizeChanged += (_, pSizeEvent) =>
            PViewerHostRecord($"host sized {pSizeEvent.NewSize.Width:0}x{pSizeEvent.NewSize.Height:0}");
        PViewerHostRecord("host built");
    }

    private void PViewerKeyForwardAttach()
    {
        Window? pViewerKeySurface = pViewerFlyleafHost.Surface;
        if (pViewerKeySurface is not null)
        {
            pViewerKeySurface.PreviewKeyDown -= PViewerHostKeyHandle;
            pViewerKeySurface.PreviewKeyDown += PViewerHostKeyHandle;
        }

        Window? pViewerKeyOverlay = pViewerFlyleafHost.Overlay;
        if (pViewerKeyOverlay is not null)
        {
            pViewerKeyOverlay.PreviewKeyDown -= PViewerHostKeyHandle;
            pViewerKeyOverlay.PreviewKeyDown += PViewerHostKeyHandle;
        }
    }

    private void PViewerHostKeyHandle(object sender, KeyEventArgs e)
    {
        PViewerKeyDispatch?.Invoke(e);
    }

    private void PViewerHostShow(bool pViewerHostVisible)
    {
        Visibility pViewerHostTarget = pViewerHostVisible ? Visibility.Visible : Visibility.Collapsed;
        if (pViewerFlyleafHost.Visibility == pViewerHostTarget)
        {
            return;
        }

        pViewerFlyleafHost.Visibility = pViewerHostTarget;
        pViewerCloseButton.Visibility = pViewerHostTarget;
        PViewerHostRecord($"host {(pViewerHostVisible ? "shown" : "hidden")}");
    }

    private void PViewerHostRecord(string pViewerStage)
    {
        if (!LTrace.LTraceCheck(LTraceKind.LTraceView))
        {
            pViewerHostStamp++;
            return;
        }

        Window? pViewerHostSurface = null;
        IntPtr pViewerHostHandle = IntPtr.Zero;
        bool pViewerHostDisposed = false;
        Window? pViewerHostOverlay = null;
        try
        {
            pViewerHostDisposed = pViewerFlyleafHost.Disposed;
            pViewerHostSurface = pViewerFlyleafHost.Surface;
            pViewerHostHandle = pViewerFlyleafHost.SurfaceHandle;
            pViewerHostOverlay = pViewerFlyleafHost.Overlay;
        }
        catch
        {
        }

        LTrace.LTraceRecord(
            LTraceKind.LTraceView,
            $"Viewer host [{++pViewerHostStamp}] {pViewerStage}",
            $"panel visible {IsVisible}, host visible {pViewerFlyleafHost.IsVisible}, command {(pViewerCommandActive ? "on" : "off")}\n"
            + $"surface {(pViewerHostSurface is null ? "none" : $"{pViewerHostSurface.Visibility} {pViewerHostSurface.Width:0}x{pViewerHostSurface.Height:0}")}, "
            + $"handle {(pViewerHostHandle == IntPtr.Zero ? "none" : "set")}, disposed {pViewerHostDisposed}\n"
            + $"overlay {(pViewerHostOverlay is null ? "none" : $"present, AllowsTransparency {pViewerHostOverlay.AllowsTransparency} (software-composited layer over the video)")}\n"
            + $"player {(pViewerPlayer is null ? "none" : "ready")}, renderer {(pViewerPlayer?.Renderer is null ? "none" : "ready")}");
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
            LMediaProbe.LMediaProbeReady -= PViewerProbeReadyHandle;
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

    public bool PViewerMediaClose(bool pViewerForce = false)
    {
        if (pViewerUnloaded || (!pViewerForce && !pViewerCommandActive))
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

        LTraceLog.LTraceInfoRecord(string.IsNullOrWhiteSpace(pViewerClosedPath)
            ? "Media closed"
            : $"Media closed '{System.IO.Path.GetFileName(pViewerClosedPath)}' [{pViewerClosedPath}]");

        PViewerMediaRaise(new LCargo(string.Empty, null, false, false, null, null));
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

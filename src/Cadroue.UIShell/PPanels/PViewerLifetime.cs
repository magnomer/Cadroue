using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private int pViewerHostStamp;

    internal void PViewerLoupeAttach(PSLoupe pViewerLoupeWindow)
    {
        pViewerLoupe = pViewerLoupeWindow;
        PViewerHostShow(false);
        PViewerCommandSet(false);
    }

    internal void PViewerLoupeDetach(TimeSpan pViewerPosition, bool pViewerPlaying)
    {
        pViewerLoupe = null;
        pViewerResumeInactive = pViewerPlaying;
        PViewerCommandSet(true);
        PViewerHostShow(true);
        PViewerSeek(pViewerPosition);
        if (pViewerPlaying)
        {
            PViewerPlay();
        }
        else
        {
            PViewerPause();
        }
    }

    private void PViewerHostBuild()
    {
        if (pViewerHostBuilt) return;

        if (PViewerMpvEligible && !pViewerEngineSubscribed)
        {
            Cadroue.Infrastructure.LRenderer.LRendererEngineChange += PViewerEngineHandle;
            pViewerEngineSubscribed = true;
        }

        if (PViewerEngineRead() == LPreviewEngine.LPreviewEngineMpv)
        {
            PViewerMpvBuild();
        }
        else
        {
            PViewerFlyleafBuild();
        }

        pViewerHostBuilt = true;
    }

    private void PViewerFlyleafBuild()
    {
        PViewerOverlayDetach();
        var pViewerOverlayHost = new Grid();
        pViewerOverlayHost.Children.Add(pViewerOverlay);
        pViewerOverlayHost.Children.Add(pViewerCloseButton);
        pViewerOverlayHost.Children.Add(pViewerPreviewButton);
        pViewerOverlayHost.Children.Add(pViewerAudioSwitch);
        pViewerOverlayHost.Children.Add(pViewerEngineOverlay);

        pViewerFlyleafHost = new FlyleafHost
        {
            Content = pViewerOverlayHost,
            VideoBackground = Brushes.White,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None,
            Visibility = Visibility.Collapsed
        };

        var pViewerFrame = new Grid();
        pViewerFrame.Children.Add(pViewerFlyleafHost);
        pViewerFrame.Children.Add(pViewerEngineSurface);

        pViewerSurface = new Border
        {
            Margin = PPanelOuterMargin,
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(0),
            Child = pViewerFrame,
            AllowDrop = true,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };

        Content = pViewerSurface;
        PViewerEngineSet(LPreviewEngine.LPreviewEngineFlyleaf);
        PViewerHostAttach();
    }

    private void PViewerHostAttach()
    {
        if (pViewerFlyleafHost is null) return;

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
        if (pViewerMpvActive)
        {
            if (pViewerMpvHost is null) return;

            Visibility pViewerMpvTarget = pViewerHostVisible ? Visibility.Visible : Visibility.Collapsed;
            pViewerMpvHost.Visibility = pViewerMpvTarget;
            pViewerCloseButton.Visibility = pViewerMpvTarget;
            pViewerPreviewButton.Visibility = pViewerMpvTarget;
            PViewerAudioShow(pViewerHostVisible);
            PViewerOverlayPlace();
            return;
        }

        if (pViewerFlyleafHost is null) return;

        PViewerAudioShow(pViewerHostVisible);

        Visibility pViewerHostTarget = pViewerHostVisible ? Visibility.Visible : Visibility.Collapsed;
        if (pViewerFlyleafHost.Visibility == pViewerHostTarget)
        {
            return;
        }

        pViewerFlyleafHost.Visibility = pViewerHostTarget;
        pViewerCloseButton.Visibility = pViewerHostTarget;
        pViewerPreviewButton.Visibility = pViewerHostTarget;
        PViewerHostRecord($"host {(pViewerHostVisible ? "shown" : "hidden")}");
    }

    private void PViewerHostRecord(string pViewerStage)
    {
        if (pViewerFlyleafHost is null || !LTrace.LTraceCheck(LTraceKind.LTraceUi))
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
            LTraceKind.LTraceUi,
            $"Viewer host [{++pViewerHostStamp}] {pViewerStage}",
            $"panel visible {IsVisible}, host visible {pViewerFlyleafHost.IsVisible}, command {(pViewerCommandActive ? "on" : "off")}\n"
            + $"surface {(pViewerHostSurface is null ? "none" : $"{pViewerHostSurface.Visibility} {pViewerHostSurface.Width:0}x{pViewerHostSurface.Height:0}")}, "
            + $"handle {(pViewerHostHandle == IntPtr.Zero ? "none" : "set")}, disposed {pViewerHostDisposed}\n"
            + $"overlay {(pViewerHostOverlay is null ? "none" : $"present, AllowsTransparency {pViewerHostOverlay.AllowsTransparency} (software-composited layer over the video)")}\n"
            + $"player {(pViewerPlayer.PPlayerReady ? "ready" : "none")}, renderer {(pViewerPlayer.PPlayerFlyleafPlayer?.Renderer is null ? "none" : "ready")}");
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
            Cadroue.Infrastructure.LRenderer.LRendererEngineChange -= PViewerEngineShow;
            if (pViewerEngineSubscribed)
            {
                Cadroue.Infrastructure.LRenderer.LRendererEngineChange -= PViewerEngineHandle;
                pViewerEngineSubscribed = false;
            }

            pViewerMediaProbe.LMediaLoadCompleted -= PViewerLoadHandle;
            pViewerMediaProbe.Dispose();
            pViewerClockTimer.Tick -= PViewerClockHandle;
            pViewerOverlay.MouseLeftButtonDown -= PCropPressHandle;
            pViewerOverlay.MouseMove -= PCropMoveHandle;
            pViewerOverlay.MouseLeftButtonUp -= PCropReleaseHandle;
            pViewerOverlay.SizeChanged -= PCropSizeHandle;
            PDropHandlersRemove();
            PPlayerStopDispose();
            PViewerFlyleafDispose();
            PViewerMpvDispose();
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

        if (pViewerLoupe is { } pViewerLoupeWindow)
        {
            pViewerLoupeWindow.Close();
        }

        bool pViewerLoadClosed = pViewerMediaProbe.LMediaLoadClose();
        if (!pViewerLoadClosed && string.IsNullOrWhiteSpace(PViewerSourcePath) && pViewerMediaInfo is null)
        {
            return false;
        }

        string pViewerClosedPath = PViewerSourcePath ?? string.Empty;
        pViewerLoadSerial++;
        pViewerLoadPath = null;
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
        if (!pViewerHostBuilt || pViewerFlyleafHost is null || pViewerSurface is null) return;

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

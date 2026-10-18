using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaFramework.MediaRenderer;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

// The viewer's Flyleaf host: it builds the host, creates the Flyleaf player, reports the host's facts and disposes it.
internal sealed class PViewerFlyleaf
{
    private static readonly IReadOnlyDictionary<bool, Action<Config>> pViewerProcessors =
        new Dictionary<bool, Action<Config>>
        {
            [true] = pConfig =>
            {
                pConfig.Video.VideoProcessor = VideoProcessors.Flyleaf;
                pConfig.Video.SyncVPFilters = false;
            },
            [false] = _ => { },
        };

    private readonly QViewer pViewerFlyleafSource;

    internal PViewerFlyleaf(QViewer lViewer, Grid pOverlayHost)
    {
        pViewerFlyleafSource = lViewer;
        PViewerFlyleafHost = PViewerFlyleafBuild(pOverlayHost);
    }

    internal FlyleafHost PViewerFlyleafHost { get; }

    private static FlyleafHost PViewerFlyleafBuild(Grid pOverlayHost) => new()
    {
        Content = pOverlayHost,
        VideoBackground = Brushes.White,
        ToggleFullScreenOnDoubleClick = AvailableWindows.None,
        AttachedDragMove = AttachedDragMoveOptions.None,
        Visibility = Visibility.Collapsed
    };

    internal void PViewerTraceAttach()
    {
        QViewerHost lHost = pViewerFlyleafSource.QViewerHost;
        PViewerFlyleafHost.Loaded += (_, _) => lHost.QViewerHostRecord("host loaded");
        PViewerFlyleafHost.IsVisibleChanged += (_, pEvent) =>
            lHost.QViewerHostRecord($"host visible {pEvent.NewValue}");
        PViewerFlyleafHost.SizeChanged += (_, pEvent) =>
            lHost.QViewerHostRecord($"host sized {pEvent.NewSize.Width:0}x{pEvent.NewSize.Height:0}");
    }

    internal void PFlyleafInertApply()
    {
        PViewerOverlay.PViewerInertApply(PPlayerFlyleaf.PPlayerHandleRead(PViewerFlyleafHost.Surface));
        PViewerOverlay.PViewerInertApply(PPlayerFlyleaf.PPlayerHandleRead(PViewerFlyleafHost.Overlay));
    }

    internal void PViewerFlyleafCreate()
    {
        Config pConfig = PViewerConfigCreate();
        QPlayer lPlayer = pViewerFlyleafSource.QPlayer;
        lPlayer.QPlayerEngineSet(new PPlayerFlyleaf(PViewerFlyleafHost, lPlayer, pConfig).PPlayerSeamRead());
    }

    private Config PViewerConfigCreate()
    {
        var pConfig = new Config();
        pConfig.Player.KeyBindings.Keys.Clear();
        pViewerProcessors[pViewerFlyleafSource.QViewerProcessorForced](pConfig);
        pConfig.Video.BackColor = Colors.White;
        pConfig.Video.ClearScreen = false;
        return pConfig;
    }

    internal QViewerHostFacts PViewerFactsRead(bool pPanelVisible)
    {
        Window? pSurface = null;
        nint pHandle = nint.Zero;
        bool pDisposed = false;
        Window? pOverlay = null;
        try
        {
            pDisposed = PViewerFlyleafHost.Disposed;
            pSurface = PViewerFlyleafHost.Surface;
            pHandle = PViewerFlyleafHost.SurfaceHandle;
            pOverlay = PViewerFlyleafHost.Overlay;
        }
        catch
        {
        }

        return new QViewerHostFacts(
            pPanelVisible,
            PViewerFlyleafHost.IsVisible,
            pSurface?.Visibility.ToString(),
            pSurface?.Width,
            pSurface?.Height,
            pHandle,
            pDisposed,
            pOverlay?.AllowsTransparency);
    }

    internal bool PViewerSurfaceMatch(nint pForeground)
    {
        try
        {
            return pViewerFlyleafSource.QViewerSurfaceMatch(
                pForeground,
                PPlayerFlyleaf.PPlayerHandleRead(PViewerFlyleafHost.Surface),
                PPlayerFlyleaf.PPlayerHandleRead(PViewerFlyleafHost.Overlay));
        }
        catch
        {
            return false;
        }
    }

    internal void PViewerFlyleafDispose()
    {
        PViewerFlyleafHost.Player = null;
        PViewerFlyleafHost.Content = null;
        ((IDisposable)PViewerFlyleafHost).Dispose();
    }
}

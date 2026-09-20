using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed partial class PSLoupe
{
    private UIElement PSLoupePlayBuild()
    {
        psLoupePlayImage = new Image
        {
            Source = PIcon.PIconRead(PSLoupeStartIcon, PSLoupePlayBrush),
            Width = PSLoupeIconSize,
            Height = PSLoupeIconSize,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        psLoupePlayButton = new Button
        {
            Width = PSLoupeButtonSize,
            Height = PSLoupeButtonSize,
            Content = psLoupePlayImage,
            Style = PButton.PButtonPanelCreate(),
            ToolTip = LLocalization.LLocalizationTextRead("Loupe.Play.Tooltip")
        };
        psLoupePlayButton.Click += PSLoupePlayHandle;
        return psLoupePlayButton;
    }

    private async void PSLoupePlaybackStart()
    {
        LSLoupe.LSLoupeSourceSet(psLoupeSource.LViewer.LViewerSourcePath);
        if (psLoupeMediaHost is null || LSLoupe.LSLoupeSource is not { } pSource || string.IsNullOrWhiteSpace(pSource))
        {
            return;
        }

        bool pLoupeMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;

        try
        {
            if (pLoupeMpv)
            {
                PSLoupeMpvBuild();
            }
            else
            {
                PSLoupeFlyleafBuild();
            }

            await LPlayer.LPlayerOpenStart(pSource);
            if (LSLoupe.LSLoupeClosed)
            {
                return;
            }

            PSLoupePreviewApply();
            LPlayer.LPlayerVolumeSet(psLoupeSource.LViewer.LViewerVolume);

            TimeSpan pInherit = psLoupeSource.LViewer.LViewerPosition;
            if (pInherit > TimeSpan.Zero)
            {
                LPlayer.LPlayerSeek(pInherit);
            }

            if (psLoupeSource.LViewer.LViewerPlaying)
            {
                LPlayer.LPlayerPlay();
                LSLoupe.LSLoupePlayingSet(true);
            }
            else
            {
                LPlayer.LPlayerPause();
                LSLoupe.LSLoupePlayingSet(false);
            }
        }
        catch
        {
            LSLoupe.LSLoupePlayingSet(false);
        }
    }

    private void PSLoupePreviewHandle()
    {
        if (LSLoupe.LSLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        PSLoupePreviewApply();
    }

    private void PSLoupePreviewApply()
    {
        LPreviewState pState = psLoupeSource.LViewer.LViewerRenderRead();
        LPlayer.LPlayerFilterApply(LPreview.LPreviewFilterResolve(pState));
        LPlayer.LPlayerAudioApply(psLoupeSource.LViewer.LViewerAudioResolve());
        LPlayer.LPlayerPreviewApply(pState, "preview color/geometry");
    }

    private void PSLoupeFlyleafBuild()
    {
        var pLoupeConfig = new Config();
        pLoupeConfig.Player.KeyBindings.Keys.Clear();
        psLoupeFlyleafHost = new FlyleafHost
        {
            VideoBackground = Brushes.Black,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None
        };
        psLoupeMediaHost!.Child = psLoupeFlyleafHost;
        LPlayer.LPlayerEngineSet(new PPlayerFlyleaf(psLoupeFlyleafHost, LPlayer, pLoupeConfig).PPlayerSeamRead());
    }

    private void PSLoupeMpvBuild()
    {
        psLoupeMpvHost = new PViewerMpvHost();
        psLoupeMediaHost!.Child = psLoupeMpvHost;
        if (psLoupeMpvHost.PViewerMpvHwnd == nint.Zero)
        {
            psLoupeMpvHost.UpdateLayout();
        }

        nint pLoupeHandle = psLoupeMpvHost.PViewerMpvHwnd;
        if (pLoupeHandle == nint.Zero)
        {
            throw new InvalidOperationException("mpv host handle not realized");
        }

        LPlayer.LPlayerEngineSet(new LPlayerMpv(pLoupeHandle).LPlayerSeamRead());
    }

    private void PSLoupePlayHandle(object pSender, RoutedEventArgs pEvent)
    {
        if (LSLoupe.LSLoupePlaying)
        {
            psLoupeSource.LViewer.LViewerPlayback.LViewerPause();
        }
        else
        {
            psLoupeSource.LViewer.LViewerPlayback.LViewerPlay();
        }
    }

    internal void PSLoupePlay()
    {
        if (LSLoupe.LSLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        if (LSLoupe.LSLoupeEnded)
        {
            LPlayer.LPlayerSeek(TimeSpan.Zero);
            LSLoupe.LSLoupeEndSet(false);
        }

        LPlayer.LPlayerPlay();
        LSLoupe.LSLoupePlayingSet(true);
    }

    internal void PSLoupePause()
    {
        if (LSLoupe.LSLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        LPlayer.LPlayerPause();
        LSLoupe.LSLoupePlayingSet(false);
    }

    internal void PSLoupeSeek(TimeSpan pPosition)
    {
        if (LSLoupe.LSLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        LSLoupe.LSLoupeEndSet(false);
        LPlayer.LPlayerSeek(pPosition);
    }

    internal void PSLoupeVolumeSet(double pVolume)
    {
        if (LSLoupe.LSLoupeClosed || !LPlayer.LPlayerReady)
        {
            return;
        }

        LPlayer.LPlayerVolumeSet(pVolume);
    }

    private void PSLoupeClockHandle(object? pSender, EventArgs pEvent)
    {
        if (LSLoupe.LSLoupeClosed || !LSLoupe.LSLoupePlaying)
        {
            return;
        }

        if (LPlayer.LPlayerEndedRead())
        {
            LSLoupe.LSLoupeEndSet(true);
            LSLoupe.LSLoupePlayingSet(false);
            return;
        }

        if (LPlayer.LPlayerReady)
        {
            psLoupeSource.LViewer.LViewerPlayback.LViewerLoupeSync(LPlayer.LPlayerTimeRead(), true);
        }
    }

    private void PSLoupePlayingHandle(bool pPlaying)
    {
        if (pPlaying)
        {
            psLoupeClock.Start();
        }
        else
        {
            psLoupeClock.Stop();
        }

        if (!LSLoupe.LSLoupeClosed)
        {
            psLoupeSource.LViewer.LViewerPlayback.LViewerLoupeSync(LPlayer.LPlayerTimeRead(), pPlaying);
        }

        if (psLoupePlayImage is not null)
        {
            psLoupePlayImage.Source = pPlaying
                ? PIcon.PIconRead(PSLoupePauseIcon, null)
                : PIcon.PIconRead(PSLoupeStartIcon, PSLoupePlayBrush);
        }

        if (psLoupePlayButton is not null)
        {
            psLoupePlayButton.ToolTip = LLocalization.LLocalizationTextRead(
                pPlaying ? "Loupe.Pause.Tooltip" : "Loupe.Play.Tooltip");
        }
    }

    private void PSLoupePlaybackDispose()
    {
        LPlayer.LPlayerDispose();
        PSLoupeFlyleafDispose();
        if (psLoupeMpvHost is not null)
        {
            psLoupeMpvHost.Dispose();
            psLoupeMpvHost = null;
        }

        if (psLoupeMediaHost is not null)
        {
            psLoupeMediaHost.Child = null;
        }
    }

    private void PSLoupeFlyleafDispose()
    {
        if (psLoupeFlyleafHost is null)
        {
            return;
        }

        try
        {
            ((IDisposable)psLoupeFlyleafHost).Dispose();
        }
        catch
        {
        }

        psLoupeFlyleafHost = null;
    }
}

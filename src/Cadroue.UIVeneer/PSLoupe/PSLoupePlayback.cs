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
        LSLoupe.LSLoupeSourceSet(psLoupeSource.PViewerSourcePath);
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

            await LPlayer.LPlayerOpenStart(pSource, psLoupePlayer.PPlayerOpen);
            if (LSLoupe.LSLoupeClosed)
            {
                return;
            }

            PSLoupePreviewApply();
            psLoupePlayer.PPlayerVolumeSet(psLoupeSource.PViewerVolumeCurrent);

            TimeSpan pInherit = psLoupeSource.PViewerPositionRead();
            if (pInherit > TimeSpan.Zero)
            {
                psLoupePlayer.PPlayerSeek(pInherit);
            }

            if (psLoupeSource.PViewerPlayingRead())
            {
                psLoupePlayer.PPlayerPlay();
                LSLoupe.LSLoupePlayingSet(true);
            }
            else
            {
                psLoupePlayer.PPlayerPause();
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
        if (LSLoupe.LSLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        PSLoupePreviewApply();
    }

    private void PSLoupePreviewApply()
    {
        LPreviewState pState = psLoupeSource.PViewerRenderRead();
        try
        {
            if (psLoupeMpvHost is not null)
            {
                psLoupePlayer.PPlayerFilterSet(LPreview.LPreviewFilterResolve(pState));
                psLoupePlayer.PPlayerAudioSet(psLoupeSource.PViewerAudioRead());
            }
            else
            {
                LPreview.LPreviewApply(psLoupePlayer.PPlayerFlyleafPlayer, pState);
            }
        }
        catch
        {
        }
    }

    private void PSLoupeFlyleafBuild()
    {
        var pLoupeFlyleafPlayer = new Player(new Config());
        pLoupeFlyleafPlayer.Config.Player.KeyBindings.Keys.Clear();
        psLoupeFlyleafHost = new FlyleafHost
        {
            Player = pLoupeFlyleafPlayer,
            VideoBackground = Brushes.Black,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None
        };
        psLoupeMediaHost!.Child = psLoupeFlyleafHost;
        psLoupePlayer.PPlayerFlyleafSet(pLoupeFlyleafPlayer);
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

        psLoupePlayer.PPlayerMpvSet(pLoupeHandle);
    }

    private void PSLoupePlayHandle(object pSender, RoutedEventArgs pEvent)
    {
        if (LSLoupe.LSLoupePlaying)
        {
            psLoupeSource.PViewerPause();
        }
        else
        {
            psLoupeSource.PViewerPlay();
        }
    }

    internal void PSLoupePlay()
    {
        if (LSLoupe.LSLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        if (LSLoupe.LSLoupeEnded)
        {
            psLoupePlayer.PPlayerSeek(TimeSpan.Zero);
            LSLoupe.LSLoupeEndSet(false);
        }

        psLoupePlayer.PPlayerPlay();
        LSLoupe.LSLoupePlayingSet(true);
    }

    internal void PSLoupePause()
    {
        if (LSLoupe.LSLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        psLoupePlayer.PPlayerPause();
        LSLoupe.LSLoupePlayingSet(false);
    }

    internal void PSLoupeSeek(TimeSpan pPosition)
    {
        if (LSLoupe.LSLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        LSLoupe.LSLoupeEndSet(false);
        psLoupePlayer.PPlayerSeek(pPosition);
    }

    internal void PSLoupeVolumeSet(double pVolume)
    {
        if (LSLoupe.LSLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        psLoupePlayer.PPlayerVolumeSet(pVolume);
    }

    private void PSLoupeClockHandle(object? pSender, EventArgs pEvent)
    {
        if (LSLoupe.LSLoupeClosed || !LSLoupe.LSLoupePlaying)
        {
            return;
        }

        if (psLoupePlayer.PPlayerEndedRead())
        {
            LSLoupe.LSLoupeEndSet(true);
            LSLoupe.LSLoupePlayingSet(false);
            return;
        }

        if (psLoupePlayer.PPlayerReady)
        {
            psLoupeSource.PViewerLoupeSync(psLoupePlayer.PPlayerTimeRead(), true);
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
            TimeSpan pPosition = psLoupePlayer.PPlayerReady ? psLoupePlayer.PPlayerTimeRead() : TimeSpan.Zero;
            psLoupeSource.PViewerLoupeSync(pPosition, pPlaying);
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
        psLoupePlayer.PPlayerDispose();
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
            psLoupeFlyleafHost.Player = null;
            ((IDisposable)psLoupeFlyleafHost).Dispose();
        }
        catch
        {
        }

        psLoupeFlyleafHost = null;
    }
}

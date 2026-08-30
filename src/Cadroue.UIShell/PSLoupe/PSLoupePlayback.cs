using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

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
        string? pSource = psLoupeSource.PViewerSourcePath;
        if (psLoupeMediaHost is null || string.IsNullOrWhiteSpace(pSource))
        {
            return;
        }

        bool pLoupeMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv
            && LMpv.LMpvInstalledCheck();

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

            await System.Threading.Tasks.Task.Run(() => psLoupePlayer.PPlayerOpen(pSource));
            if (psLoupeClosed)
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
                PSLoupePlayingSet(true);
            }
            else
            {
                psLoupePlayer.PPlayerPause();
                PSLoupePlayingSet(false);
            }
        }
        catch
        {
            PSLoupePlayingSet(false);
        }
    }

    private void PSLoupePreviewHandle()
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
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
        if (psLoupePlaying)
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
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        if (psLoupeEnded)
        {
            psLoupePlayer.PPlayerSeek(TimeSpan.Zero);
            psLoupeEnded = false;
        }

        psLoupePlayer.PPlayerPlay();
        PSLoupePlayingSet(true);
    }

    internal void PSLoupePause()
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        psLoupePlayer.PPlayerPause();
        PSLoupePlayingSet(false);
    }

    internal void PSLoupeSeek(TimeSpan pPosition)
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        psLoupeEnded = false;
        psLoupePlayer.PPlayerSeek(pPosition);
    }

    internal void PSLoupeVolumeSet(double pVolume)
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        psLoupePlayer.PPlayerVolumeSet(pVolume);
    }

    private void PSLoupeClockHandle(object? pSender, EventArgs pEvent)
    {
        if (psLoupeClosed || !psLoupePlaying)
        {
            return;
        }

        if (psLoupePlayer.PPlayerEndedRead())
        {
            psLoupeEnded = true;
            PSLoupePlayingSet(false);
            return;
        }

        if (psLoupePlayer.PPlayerReady)
        {
            psLoupeSource.PViewerLoupeSync(psLoupePlayer.PPlayerTimeRead(), true);
        }
    }

    private void PSLoupePlayingSet(bool pPlaying)
    {
        psLoupePlaying = pPlaying;
        if (pPlaying)
        {
            psLoupeEnded = false;
            psLoupeClock.Start();
        }
        else
        {
            psLoupeClock.Stop();
        }

        if (!psLoupeClosed)
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
        psLoupePlayer.PPlayerMpvCancel();
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

using System.Windows;
using Cadroue.Media;
using FlyleafLib;
using FlyleafLib.MediaPlayer;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewerPanel
{
    private async Task PViewerPanelVideoLoadAsynchronous(string sourcePath)
    {
        if (!pViewerPanelCommandActive) return;
        int loadSerial = ++pViewerPanelLoadSerial;
        pViewerPanelClockTimer.Stop();
        PViewerPanelPlayerStopDispose();
        PViewerPanelSourcePathCurrent = null;
        pViewerPanelMediaInfo = null;
        PViewerPanelCropBoxVideo = null;
        LPreviewStateCurrent = LPreviewStateCurrent.LCropBoxChange(null).LPlaybackStateChange(LPlaybackState.LPlaybackStateStoppedCreate());
        PViewerPanelCropBoxHide();
        if (loadSerial != pViewerPanelLoadSerial || pViewerPanelUnloaded || !pViewerPanelCommandActive)
        {
            return;
        }

        LMediaInfo? mediaInfo = null;
        string? ffmpegError = null;
        try
        {
            mediaInfo = LMediaInfo.LMediaInfoFfprobeRequest(sourcePath);
            if (mediaInfo.LMediaInfoAudioOnly && !pViewerPanelAudioOnlyAllowed)
            {
                const string audioOnlyError = "Audio-only files can be opened only in the Audio tab.";
                PViewerPanelMediaStatusCommit(new LMediaOpenStatus(
                    sourcePath, null, false, false, audioOnlyError, audioOnlyError), null);
                return;
            }
        }
        catch (Exception exception)
        {
            ffmpegError = exception.Message;
        }

        Player? player = null;
        string? previewError = null;
        try
        {
            player = new Player(new Config());
            player.Audio.Volume = (int)Math.Round(pViewerPanelVolume);
            PViewerPanelPlayerOpen(player, sourcePath);
        }
        catch (Exception exception)
        {
            previewError = exception.Message;
            PViewerPanelPlayerDispose(player);
            player = null;
        }

        if (loadSerial != pViewerPanelLoadSerial || pViewerPanelUnloaded || !pViewerPanelCommandActive)
        {
            PViewerPanelPlayerDispose(player);
            return;
        }

        LMediaOpenStatus mediaStatus = new(
            sourcePath,
            mediaInfo,
            mediaInfo is not null,
            player is not null,
            ffmpegError,
            previewError);
        PViewerPanelMediaStatusCommit(mediaStatus, player);
        await Task.CompletedTask.ConfigureAwait(true);
    }

    private static void PViewerPanelPlayerOpen(Player player, string sourcePath)
    {
        var openResult = player.Open(sourcePath);
        if (!openResult.Success)
        {
            throw new InvalidOperationException(openResult.Error ?? "Flyleaf could not open the media file.");
        }
    }

    private void PViewerPanelMediaStatusCommit(LMediaOpenStatus mediaStatus, Player? player)
    {
        pViewerPanelPlayer = player;
        pViewerPanelFlyleafHost.Player = player;
        pViewerPanelMediaInfo = mediaStatus.LMediaOpenMediaInfo;
        PViewerPanelSourcePathCurrent = mediaStatus.LMediaOpenSourcePath;
        PViewerPanelPreviewStateApply();
        PViewerPanelMediaStatusChangeRaise(mediaStatus);
        if (player is null)
        {
            PViewerPanelPlaybackStateUpdate(false, TimeSpan.Zero);
            return;
        }

        if (App.LPreferenceStateCurrent.LPreferenceAutoplayOnLoad)
        {
            pViewerPanelResumeAfterInactive = false;
            player.Play();
            PViewerPanelPlaybackStateUpdate(true, PViewerPanelPlayerTimeRead(player));
            pViewerPanelClockTimer.Start();
        }
        else
        {
            PViewerPanelPlayerPauseAtStart(player);
            PViewerPanelPlaybackStateUpdate(false, TimeSpan.Zero);
        }
    }

    private static void PViewerPanelPlayerPauseAtStart(Player player)
    {
        player.Pause();
        player.Seek(0);
    }

    private void PViewerPanelPlaybackSuspendForInactive()
    {
        pViewerPanelClockTimer.Stop();
        if (pViewerPanelPlayer is null)
        {
            pViewerPanelResumeAfterInactive = false;
            return;
        }

        pViewerPanelResumeAfterInactive = LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;
        if (!pViewerPanelResumeAfterInactive)
        {
            return;
        }

        pViewerPanelPlayer.Pause();
    }

    private void PViewerPanelPlaybackResumeForActive()
    {
        if (!pViewerPanelResumeAfterInactive || pViewerPanelPlayer is null)
        {
            pViewerPanelResumeAfterInactive = false;
            if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
            {
                pViewerPanelClockTimer.Start();
            }
            return;
        }

        pViewerPanelResumeAfterInactive = false;
        pViewerPanelPlayer.Play();
        PViewerPanelPlaybackStateUpdate(true, PViewerPanelPlayerTimeRead(pViewerPanelPlayer));
        pViewerPanelClockTimer.Start();
    }

    private void PViewerPanelMediaStatusChangeRaise(LMediaOpenStatus mediaStatus)
    {
        try
        {
            PViewerPanelMediaStatusChange?.Invoke(mediaStatus);
        }
        catch
        {
        }
    }

    private void PViewerPanelClockTickHandle(object? sender, EventArgs eventArgs)
    {
        if (!pViewerPanelCommandActive || pViewerPanelPlayer is null)
        {
            return;
        }

        TimeSpan playbackPosition = PViewerPanelPlayerTimeRead(pViewerPanelPlayer);
        PViewerPanelPlaybackStateUpdate(null, playbackPosition);
        PViewerPanelClockTick?.Invoke(playbackPosition);
    }

    private void PViewerPanelPlaybackStateUpdate(bool? playing, TimeSpan? playbackPosition)
    {
        LPlaybackState playbackState = LPreviewStateCurrent.LPlaybackState;
        LPreviewStateCurrent = LPreviewStateCurrent.LPlaybackStateChange(new LPlaybackState(
            playing ?? playbackState.LPlaybackStatePlaying,
            playbackPosition ?? playbackState.LPlaybackPosition));
    }

    private static TimeSpan PViewerPanelPlayerTimeRead(Player player)
    {
        return TimeSpan.FromTicks(player.CurTime);
    }

    private void PViewerPanelPlayerStopDispose()
    {
        pViewerPanelClockTimer.Stop();
        pViewerPanelResumeAfterInactive = false;
        PViewerPanelPlaybackStateUpdate(false, null);
        PViewerPanelPlayerDispose(pViewerPanelPlayer);
        pViewerPanelFlyleafHost.Player = null;
        pViewerPanelPlayer = null;
    }

    private static void PViewerPanelPlayerDispose(Player? player)
    {
        if (player is null)
        {
            return;
        }

        try
        {
            player.Stop();
            player.Dispose();
        }
        catch
        {
        }
    }
}

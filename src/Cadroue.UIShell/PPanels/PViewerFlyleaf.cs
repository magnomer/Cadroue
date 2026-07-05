using System.Windows;
using Cadroue.Media;
using FlyleafLib;
using FlyleafLib.MediaPlayer;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private async Task PPlayerVideoLoad(string sourcePath)
    {
        if (!pViewerCommandActive) return;
        int loadSerial = ++pViewerLoadSerial;
        pViewerClockTimer.Stop();
        PPlayerStopDispose();
        PViewerSourcePath = null;
        pViewerMediaInfo = null;
        PCropVideo = null;
        LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(null).LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        PCropHide();
        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            return;
        }

        LMediaInfo? mediaInfo = null;
        string? ffmpegError = null;
        try
        {
            mediaInfo = LMediaInfo.LMediaFfprobeRead(sourcePath);
            if (mediaInfo.LMediaInfoAudioOnly && !pViewerAudioOnlyAllowed)
            {
                const string audioOnlyError = "Audio-only files can be opened only in the Audio tab.";
                PViewerMediaCommit(new LMediaOpenStatus(
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
            player.Audio.Volume = (int)Math.Round(pViewerVolume);
            PPlayerOpen(player, sourcePath);
        }
        catch (Exception exception)
        {
            previewError = exception.Message;
            PPlayerDispose(player);
            player = null;
        }

        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            PPlayerDispose(player);
            return;
        }

        LMediaOpenStatus mediaStatus = new(
            sourcePath,
            mediaInfo,
            mediaInfo is not null,
            player is not null,
            ffmpegError,
            previewError);
        PViewerMediaCommit(mediaStatus, player);
        await Task.CompletedTask.ConfigureAwait(true);
    }

    private static void PPlayerOpen(Player player, string sourcePath)
    {
        var openResult = player.Open(sourcePath);
        if (!openResult.Success)
        {
            throw new InvalidOperationException(openResult.Error ?? "Flyleaf could not open the media file.");
        }
    }

    private void PViewerMediaCommit(LMediaOpenStatus mediaStatus, Player? player)
    {
        pViewerPlayer = player;
        pViewerFlyleafHost.Player = player;
        pViewerMediaInfo = mediaStatus.LMediaOpenMediaInfo;
        PViewerSourcePath = mediaStatus.LMediaOpenSourcePath;
        PViewerPreviewApply();
        PViewerMediaRaise(mediaStatus);
        if (player is null)
        {
            PViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        if (App.LPreferenceStateCurrent.LPreferenceAutoplayOnLoad)
        {
            pViewerResumeAfterInactive = false;
            player.Play();
            PViewerPlaybackUpdate(true, PPlayerTimeRead(player));
            pViewerClockTimer.Start();
        }
        else
        {
            PPlayerStartPause(player);
            PViewerPlaybackUpdate(false, TimeSpan.Zero);
        }
    }

    private static void PPlayerStartPause(Player player)
    {
        player.Pause();
        player.Seek(0);
    }

    private void PPlayerSuspend()
    {
        pViewerClockTimer.Stop();
        if (pViewerPlayer is null)
        {
            pViewerResumeAfterInactive = false;
            return;
        }

        pViewerResumeAfterInactive = LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;
        if (!pViewerResumeAfterInactive)
        {
            return;
        }

        pViewerPlayer.Pause();
    }

    private void PPlayerResume()
    {
        if (!pViewerResumeAfterInactive || pViewerPlayer is null)
        {
            pViewerResumeAfterInactive = false;
            if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
            {
                pViewerClockTimer.Start();
            }
            return;
        }

        pViewerResumeAfterInactive = false;
        pViewerPlayer.Play();
        PViewerPlaybackUpdate(true, PPlayerTimeRead(pViewerPlayer));
        pViewerClockTimer.Start();
    }

    private void PViewerMediaRaise(LMediaOpenStatus mediaStatus)
    {
        try
        {
            PViewerMediaChange?.Invoke(mediaStatus);
        }
        catch
        {
        }
    }

    private void PViewerClockHandle(object? sender, EventArgs eventArgs)
    {
        if (!pViewerCommandActive || pViewerPlayer is null)
        {
            return;
        }

        TimeSpan playbackPosition = PPlayerTimeRead(pViewerPlayer);
        PViewerPlaybackUpdate(null, playbackPosition);
        PViewerClockTick?.Invoke(playbackPosition);
    }

    private void PViewerPlaybackUpdate(bool? playing, TimeSpan? playbackPosition)
    {
        LPlaybackState playbackState = LPreviewStateCurrent.LPlaybackState;
        LPreviewStateCurrent = LPreviewStateCurrent.LPlaybackStateChange(new LPlaybackState(
            playing ?? playbackState.LPlaybackStatePlaying,
            playbackPosition ?? playbackState.LPlaybackPosition));
    }

    private static TimeSpan PPlayerTimeRead(Player player)
    {
        return TimeSpan.FromTicks(player.CurTime);
    }

    private void PPlayerStopDispose()
    {
        pViewerClockTimer.Stop();
        pViewerResumeAfterInactive = false;
        PViewerPlaybackUpdate(false, null);
        PPlayerDispose(pViewerPlayer);
        pViewerFlyleafHost.Player = null;
        pViewerPlayer = null;
    }

    private static void PPlayerDispose(Player? player)
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

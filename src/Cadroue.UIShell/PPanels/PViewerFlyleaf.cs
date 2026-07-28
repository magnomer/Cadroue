using System.Windows;
using System.Windows.Threading;
using Cadroue.Media;
using FlyleafLib;
using FlyleafLib.MediaPlayer;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private static readonly System.Windows.Media.Color PViewerBackColor = System.Windows.Media.Colors.White;

    private void PPlayerAccurateSeek(Player player, TimeSpan playbackPosition)
    {
        int pPlayerSeekMilliseconds = (int)playbackPosition.TotalMilliseconds;
        if (!pPlayerAccurateActive)
        {
            pPlayerAccurateActive = true;
            player.SeekAccurate(pPlayerSeekMilliseconds);
            return;
        }

        PPlayerDecodeInterrupt(player, true);
        Dispatcher.InvokeAsync(
            () =>
            {
                PPlayerDecodeInterrupt(player, false);
                if (pViewerUnloaded || !pViewerCommandActive || !ReferenceEquals(player, pViewerPlayer)) return;
                pPlayerAccurateActive = true;
                player.SeekAccurate(pPlayerSeekMilliseconds);
            },
            DispatcherPriority.Render);
    }

    private void PPlayerSeekCompleteHandle(object? sender, int seekMilliseconds) => pPlayerAccurateActive = false;

    private static void PPlayerDecodeInterrupt(Player player, bool pPlayerInterruptRaised)
    {
        try
        {
            player.decoder.Interrupt = pPlayerInterruptRaised;
        }
        catch
        {
        }
    }

    private async Task PPlayerVideoLoad(string sourcePath)
    {
        if (!pViewerCommandActive) return;
        int loadSerial = ++pViewerLoadSerial;
        pViewerClockTimer.Stop();
        pViewerResumeAfterInactive = false;
        PPlayerQuiet(pViewerPlayer);
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

        Player? player = pViewerPlayer;
        bool pPlayerCreated = false;
        string? previewError = null;
        try
        {
            if (player is null)
            {
                player = new Player(new Config());
                player.Config.Video.BackColor = PViewerBackColor;
                player.Config.Video.ClearScreen = false;
                pPlayerCreated = true;
            }

            player.Audio.Volume = (int)Math.Round(pViewerVolume);
            PPlayerOpen(player, sourcePath);
            PPlayerStartPause(player);
        }
        catch (Exception exception)
        {
            previewError = exception.Message;
            if (pPlayerCreated)
            {
                PPlayerDispose(player);
            }

            player = null;
        }

        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            if (pPlayerCreated && !ReferenceEquals(player, pViewerPlayer))
            {
                PPlayerDispose(player);
            }

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
        PViewerMediaReport(mediaStatus, player);

        Player? pPlayerPrevious = pViewerPlayer;
        bool pPlayerReused = ReferenceEquals(pPlayerPrevious, player) && player is not null;
        pPlayerAccurateActive = false;

        if (pPlayerReused)
        {
            LAppLog.LInfo("Viewer player reused: same player kept on the host, no swap chain rebuild");
        }
        else
        {
            if (pPlayerPrevious is not null)
            {
                pPlayerPrevious.SeekCompleted -= PPlayerSeekCompleteHandle;
            }

            pViewerPlayer = player;
            if (player is not null)
            {
                player.SeekCompleted += PPlayerSeekCompleteHandle;
            }

            LAppLog.LInfo(
                $"Viewer player swapped: previous {(pPlayerPrevious is null ? "none" : "released")}, "
                + $"next {(player is null ? "none" : "ready")}, "
                + $"renderer {(player?.Renderer is null ? "none" : "ready")}");

            pViewerFlyleafHost.Player = player;
            PPlayerDispose(pPlayerPrevious);
        }

        PViewerHostShow(player is not null);
        pViewerMediaInfo = mediaStatus.LMediaOpenMediaInfo;
        PViewerSourcePath = mediaStatus.LMediaOpenSourcePath;
        LPreviewStateCurrent = LPreviewStateCurrent.LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        if (!PCropPersistent)
        {
            PCropVideo = null;
            LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(null);
            PCropHide();
        }

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
            PViewerPreviewRestore();
            PViewerPlaybackUpdate(true, PPlayerTimeRead(player));
            pViewerClockTimer.Start();
        }
        else
        {
            PViewerPreviewRestore();
            PViewerPlaybackUpdate(false, TimeSpan.Zero);
        }
    }

    private static void PViewerMediaReport(LMediaOpenStatus mediaStatus, Player? player)
    {
        string pSourcePath = mediaStatus.LMediaOpenSourcePath ?? "(no path)";
        string pFileName = System.IO.Path.GetFileName(pSourcePath);

        if (mediaStatus.LMediaOpenMediaInfo is not LMediaInfo pMediaInfo)
        {
            LAppLog.LError($"Media rejected '{pFileName}': {mediaStatus.LMediaOpenFfmpegError ?? "unreadable"} [{pSourcePath}]");
            return;
        }

        string pStreams = pMediaInfo.LMediaInfoVideoPresent
            ? $"video {pMediaInfo.LMediaInfoVideoWidth}x{pMediaInfo.LMediaInfoVideoHeight} {pMediaInfo.LMediaInfoVideoCodecName} {pMediaInfo.LMediaInfoVideoFrameRate:0.###}fps"
            : "no video";
        if (pMediaInfo.LMediaInfoAudioPresent)
        {
            pStreams += $", audio {pMediaInfo.LMediaInfoAudioCodecName} {pMediaInfo.LMediaInfoAudioSampleRate}Hz {pMediaInfo.LMediaInfoAudioChannels}ch";
        }

        LAppLog.LInfo(
            $"Media opened '{pFileName}': {pMediaInfo.LMediaInfoDuration:hh\\:mm\\:ss\\.fff}, {pStreams} [{pSourcePath}]");

        if (player is null)
        {
            LAppLog.LError($"Preview unavailable for '{pFileName}': {mediaStatus.LMediaOpenPreviewError ?? "the player did not start"}");
        }
    }

    private static void PPlayerStartPause(Player player)
    {
        player.Pause();
        player.Seek(0);
    }

    private static void PPlayerQuiet(Player? player)
    {
        if (player is null)
        {
            return;
        }

        try
        {
            player.Pause();
        }
        catch
        {
        }
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
        pPlayerAccurateActive = false;
        pViewerResumeAfterInactive = false;
        PViewerPlaybackUpdate(false, null);
        if (pViewerPlayer is { } pPlayerClosing)
        {
            pPlayerClosing.SeekCompleted -= PPlayerSeekCompleteHandle;
        }

        Player? pPlayerPrevious = pViewerPlayer;
        pViewerFlyleafHost.Player = null;
        LAppLog.LInfo($"Viewer host detached: player {(pPlayerPrevious is null ? "none" : "released")}");
        PPlayerDispose(pPlayerPrevious);
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

using System;
using System.Linq;
using System.Reflection;
using Cadroue.Media;
using FlyleafLib;
using FlyleafLib.MediaFramework.MediaRenderer;
using FlyleafLib.MediaPlayer;

using Cadroue.Core;

using Cadroue.Application;

using Cadroue.Infrastructure;


namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private static readonly System.Windows.Media.Color PViewerBackColor = System.Windows.Media.Colors.White;

    public bool PViewerColorPreview { get; set; }

    private void PPlayerAccurateSeek(TimeSpan playbackPosition)
    {
        bool pPlayerWasRunning = pPlayerAccurateActive;
        pPlayerAccurateActive = true;
        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            $"Seek accurate to {playbackPosition:hh\\:mm\\:ss\\.fff}",
            pPlayerWasRunning
                ? "a seek was still running; queued for Flyleaf to conflate"
                : "no seek was in flight");
        pViewerPlayer.PPlayerSeek(playbackPosition);
    }

    private void PPlayerSeekHandle(object? sender, int seekMilliseconds)
    {
        pPlayerAccurateActive = false;
        if (pPlayerRendererPending && seekMilliseconds >= 0 && sender is Player pPlayerSeeked)
        {
            pPlayerRendererPending = false;
            PPlayerRendererRecord(pPlayerSeeked);
        }
    }

    private static string PPlayerContrastRead(object? pRenderer)
    {
        if (pRenderer is null)
        {
            return "none";
        }

        try
        {
            FieldInfo? pBufferField = pRenderer.GetType().GetField("psData", BindingFlags.NonPublic | BindingFlags.Instance);
            object? pBuffer = pBufferField?.GetValue(pRenderer);
            FieldInfo? pContrastField = pBuffer?.GetType().GetField("Contrast");
            return pContrastField?.GetValue(pBuffer)?.ToString() ?? "unknown";
        }
        catch (Exception pException)
        {
            return $"error {pException.Message}";
        }
    }

    private static void PPlayerColorRecord(Player? player)
    {
        if (player is null || !LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            return;
        }

        try
        {
            var pPlayerRenderer = player.Renderer;
            LTrace.LTraceRecord(
                LTraceKind.LTraceUi,
                "Preview color applied",
                $"processor in use {(pPlayerRenderer is null ? "none" : pPlayerRenderer.VideoProcessor.ToString())}, "
                + $"filter contrast value {(player.Config.Video.FLFilters.TryGetValue(FLFilters.Contrast, out FLFilter? pContrastFilter) ? pContrastFilter.Value.ToString() : "none")}, "
                + $"shader contrast uniform {PPlayerContrastRead(pPlayerRenderer)}");
        }
        catch (Exception pException)
        {
            LTrace.LTraceRecord(LTraceKind.LTraceUi, "Preview color diagnostic failed", pException.Message);
        }
    }

    private void PPlayerHostRecord(Player? player)
    {
        if (pViewerFlyleafHost is null) return;

        try
        {
            var pHostType = pViewerFlyleafHost.GetType();
            var pHostAssembly = pHostType.Assembly.GetName();
            Player? pHostPlayer = pViewerFlyleafHost.Player;
            var pFields = pHostType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(pField => pField.FieldType.Name.Contains("Renderer") || pField.FieldType.Name.Contains("Player")
                    || pField.Name.ToLowerInvariant().Contains("render") || pField.Name.ToLowerInvariant().Contains("child"))
                .Select(pField => $"{pField.Name}:{pField.FieldType.Name}");

            LTraceLog.LTraceInfoRecord(
                "Host render diagnostic",
                $"host type {pHostType.FullName}\n"
                + $"host assembly {pHostAssembly.Name} {pHostAssembly.Version}\n"
                + $"host.Player is our player: {ReferenceEquals(pHostPlayer, player)}\n"
                + $"host.Player null: {pHostPlayer is null}\n"
                + $"host.Player.Renderer null: {pHostPlayer?.Renderer is null}\n"
                + $"our.Renderer == host.Player.Renderer: {ReferenceEquals(player?.Renderer, pHostPlayer?.Renderer)}\n"
                + $"player assembly ver: {player?.GetType().Assembly.GetName().Version}\n"
                + $"host player/renderer fields: [{string.Join(", ", pFields)}]");
        }
        catch (Exception pException)
        {
            LTraceLog.LTraceErrorRecord("Host render diagnostic failed", pException);
        }
    }

    private static void PPlayerRendererRecord(Player player)
    {
        if (!LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            return;
        }

        try
        {
            var pPlayerRenderer = player.Renderer;
            var pPlayerDecoder = player.decoder?.VideoDecoder;
            LTrace.LTraceRecord(
                LTraceKind.LTraceUi,
                "Renderer resolved after the first completed seek",
                $"processor requested {player.Config.Video.VideoProcessor}, "
                + $"processor in use {(pPlayerRenderer is null ? "none" : pPlayerRenderer.VideoProcessor.ToString())}\n"
                + $"decode path {(pPlayerDecoder is null ? "unknown" : pPlayerDecoder.VideoAccelerated ? "HARDWARE (D3D11VA)" : "SOFTWARE")}\n"
                + $"pixel format {(pPlayerDecoder?.VideoStream is null ? "unknown" : pPlayerDecoder.VideoStream.PixelFormatStr)}\n"
                + $"color range {(pPlayerDecoder?.VideoStream is null ? "unknown" : pPlayerDecoder.VideoStream.ColorRange.ToString())}, "
                + $"color space {(pPlayerDecoder?.VideoStream is null ? "unknown" : pPlayerDecoder.VideoStream.ColorSpace.ToString())}\n"
                + $"sync vp filters {player.Config.Video.SyncVPFilters}, "
                + $"filter contrast value {(player.Config.Video.FLFilters.TryGetValue(FLFilters.Contrast, out FLFilter? pContrastFilter) ? pContrastFilter.Value.ToString() : "none")}\n"
                + $"shader contrast uniform {PPlayerContrastRead(pPlayerRenderer)}");
        }
        catch (Exception pPlayerException)
        {
            LTrace.LTraceRecord(
                LTraceKind.LTraceUi,
                "Renderer state could not be read",
                pPlayerException.Message);
        }
    }

    private void PPlayerVideoLoad(string sourcePath)
    {
        if (!pViewerCommandActive) return;
        PViewerEngineSelect();
        int loadSerial = ++pViewerLoadSerial;
        pViewerClockTimer.Stop();
        pViewerResumeInactive = false;
        pViewerPlayer.PPlayerPause();
        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            return;
        }

        try
        {
            pViewerLoadPath = System.IO.Path.GetFullPath(sourcePath);
        }
        catch (Exception pViewerPathException) when (pViewerPathException is ArgumentException or NotSupportedException)
        {
            pViewerLoadPath = sourcePath;
        }
        _ = pViewerMediaProbe.LMediaLoadStart(sourcePath);
    }

    private void PViewerLoadHandle(LMediaLoadOutcome result)
    {
        int loadSerial = pViewerLoadSerial;
        Dispatcher.BeginInvoke(() =>
        {
            if (pViewerUnloaded
                || loadSerial != pViewerLoadSerial
                || !string.Equals(result.LMediaLoadPath, pViewerLoadPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (result.LMediaLoadKind != LMediaLoadKind.LMediaLoadSuccess)
            {
                PViewerMediaRaise(new LCargo(
                    result.LMediaLoadPath,
                    null,
                    false,
                    false,
                    result.LMediaLoadError,
                    null));
                return;
            }

            PPlayerMediaApply(
                result.LMediaLoadPath,
                result.LMediaLoadInfo,
                null,
                loadSerial);
        });
    }

    private void PPlayerMediaApply(string sourcePath, LMediaInfo? mediaInfo, string? ffmpegError, int loadSerial)
    {
        if (pViewerMpvActive)
        {
            PViewerMpvApply(sourcePath, mediaInfo, ffmpegError, loadSerial);
            return;
        }

        if (mediaInfo is { LMediaAudioOnly: true } && !pViewerAudioAllowed)
        {
            string audioOnlyError = LLocalization.LLocalizationTextRead("Viewer.Error.AudioOnlyTab");
            PViewerMediaCommit(new LCargo(
                sourcePath, null, false, false, audioOnlyError, audioOnlyError), null);
            return;
        }

        Player? player = pViewerPlayer.PPlayerFlyleafPlayer;
        bool pPlayerCreated = false;
        string? previewError = null;
        var pPlayerClock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            if (player is null)
            {
                player = new Player(new Config());
                if (PViewerColorPreview && LFlyleaf.LFlyleafActive)
                {
                    player.Config.Video.VideoProcessor = VideoProcessors.Flyleaf;
                    player.Config.Video.SyncVPFilters = false;
                }

                player.Config.Video.BackColor = PViewerBackColor;
                player.Config.Video.ClearScreen = false;
                player.SeekCompleted += PPlayerSeekHandle;
                pPlayerCreated = true;
                LTrace.LTraceRecord(
                    LTraceKind.LTraceUi,
                    "Player created",
                    PPlayerConfigRead(player),
                    pPlayerClock.Elapsed.TotalMilliseconds);
            }

            player.Audio.Volume = (int)Math.Round(pViewerVolume);
            double pPlayerBeforeOpen = pPlayerClock.Elapsed.TotalMilliseconds;
            PPlayerFlyleafOpen(player, sourcePath);
            pPlayerRendererPending = true;
            LTrace.LTraceRecord(
                LTraceKind.LTraceUi,
                $"Player opened '{System.IO.Path.GetFileName(sourcePath)}'",
                PPlayerAccelRead(player),
                pPlayerClock.Elapsed.TotalMilliseconds - pPlayerBeforeOpen);
            PPlayerStartPause(player);
        }
        catch (Exception exception)
        {
            previewError = exception.Message;
            if (pPlayerCreated)
            {
                PPlayerFlyleafDispose(player);
            }

            player = null;
        }

        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            if (pPlayerCreated && !ReferenceEquals(player, pViewerPlayer.PPlayerFlyleafPlayer))
            {
                PPlayerFlyleafDispose(player);
            }

            return;
        }

        LCargo mediaStatus = new(
            sourcePath,
            mediaInfo,
            mediaInfo is not null,
            player is not null,
            ffmpegError,
            previewError);
        PViewerMediaCommit(mediaStatus, player);
    }

    private static void PPlayerFlyleafOpen(Player player, string sourcePath)
    {
        var openResult = player.Open(sourcePath);
        if (!openResult.Success)
        {
            throw new InvalidOperationException(openResult.Error ?? LLocalization.LLocalizationTextRead("Viewer.Error.FlyleafOpen"));
        }
    }

    private static void PPlayerFlyleafDispose(Player? player)
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

    private static string PPlayerConfigRead(Player player)
    {
        try
        {
            return $"video acceleration requested {player.Config.Video.VideoAcceleration}, "
                + $"processor requested {player.Config.Video.VideoProcessor}\n"
                + $"max output {player.Config.Video.MaxOutputFps}fps, "
                + $"decoder threads {player.Config.Decoder.VideoThreads}, "
                + $"max video frames {player.Config.Decoder.MaxVideoFrames}\n"
                + $"clear screen {player.Config.Video.ClearScreen}, sws forced {player.Config.Video.SwsForce}";
        }
        catch (Exception pPlayerException)
        {
            return $"player configuration could not be read: {pPlayerException.Message}";
        }
    }

    private static string PPlayerAccelRead(Player player)
    {
        try
        {
            var pPlayerDecoder = player.decoder?.VideoDecoder;
            var pPlayerRenderer = player.Renderer;
            string pPlayerAccel = pPlayerDecoder is null
                ? "decoder not ready"
                : pPlayerDecoder.VideoAccelerated
                    ? "HARDWARE (D3D11VA)"
                    : "SOFTWARE — hardware decode did not engage";

            return $"decode path: {pPlayerAccel}\n"
                + $"acceleration requested {player.Config.Video.VideoAcceleration}, "
                + $"processor requested {player.Config.Video.VideoProcessor}\n"
                + $"adapter {(pPlayerRenderer?.GPUAdapter is null ? "none" : pPlayerRenderer.GPUAdapter.Description)}\n"
                + "processor in use and pixel format follow once the first seek completes";
        }
        catch (Exception pPlayerException)
        {
            return $"acceleration state could not be read: {pPlayerException.Message}";
        }
    }

    private void PViewerMediaCommit(LCargo mediaStatus, Player? player)
    {
        PViewerMediaRecord(mediaStatus, player);

        Player? pPlayerPrevious = pViewerPlayer.PPlayerFlyleafPlayer;
        bool pPlayerReused = ReferenceEquals(pPlayerPrevious, player) && player is not null;
        pPlayerAccurateActive = false;

        if (pPlayerReused)
        {
            LTraceLog.LTraceInfoRecord("Viewer player reused: same player kept on the host, no swap chain rebuild");
        }
        else
        {
            if (pPlayerPrevious is not null)
            {
                pPlayerPrevious.SeekCompleted -= PPlayerSeekHandle;
            }

            LTraceLog.LTraceInfoRecord(
                $"Viewer player swapped: previous {(pPlayerPrevious is null ? "none" : "released")}, "
                + $"next {(player is null ? "none" : "ready")}, "
                + $"renderer {(player?.Renderer is null ? "none" : "ready")}");

            if (pViewerFlyleafHost is not null) pViewerFlyleafHost.Player = player;
            PPlayerHostRecord(player);
            if (player is null)
            {
                pViewerPlayer.PPlayerDispose();
            }
            else
            {
                pViewerPlayer.PPlayerFlyleafSet(player);
            }
        }

        PViewerHostShow(player is not null);
        pViewerMediaInfo = mediaStatus.LCargoMediaInfo;
        PViewerSourcePath = mediaStatus.LCargoSourcePath;
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

        if (LPreference.LPreferenceStateCurrent.LPreferenceAutoplay)
        {
            pViewerResumeInactive = false;
            pViewerPlayer.PPlayerPlay();
            PViewerPreviewRestore();
            PViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
            pViewerClockTimer.Start();
        }
        else
        {
            PViewerPreviewRestore();
            PViewerPlaybackUpdate(false, TimeSpan.Zero);
        }
    }

    private static void PViewerMediaRecord(LCargo mediaStatus, Player? player)
    {
        string pSourcePath = mediaStatus.LCargoSourcePath ?? "(no path)";
        string pFileName = System.IO.Path.GetFileName(pSourcePath);

        if (mediaStatus.LCargoMediaInfo is not LMediaInfo pMediaInfo)
        {
            LTraceLog.LTraceErrorRecord($"Media rejected '{pFileName}': {mediaStatus.LCargoFfmpegError ?? "unreadable"} [{pSourcePath}]");
            return;
        }

        string pStreams = pMediaInfo.LMediaVideoPresent
            ? $"video {pMediaInfo.LMediaVideoWidth}x{pMediaInfo.LMediaVideoHeight} {pMediaInfo.LMediaVideoCodec} {pMediaInfo.LMediaVideoRate:0.###}fps"
            : "no video";
        if (pMediaInfo.LMediaAudioPresent)
        {
            pStreams += $", audio {pMediaInfo.LMediaAudioCodec} {pMediaInfo.LMediaSampleRate}Hz {pMediaInfo.LMediaAudioChannels}ch";
        }

        LTraceLog.LTraceInfoRecord(
            $"Media opened '{pFileName}': {pMediaInfo.LMediaInfoDuration:hh\\:mm\\:ss\\.fff}, {pStreams} [{pSourcePath}]");

        if (player is null)
        {
            LTraceLog.LTraceErrorRecord($"Preview unavailable for '{pFileName}': {mediaStatus.LCargoPreviewError ?? "the player did not start"}");
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
        if (!pViewerPlayer.PPlayerReady)
        {
            pViewerResumeInactive = false;
            return;
        }

        pViewerResumeInactive = LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;
        if (!pViewerResumeInactive)
        {
            return;
        }

        pViewerPlayer.PPlayerPause();
    }

    private void PPlayerResume()
    {
        if (!pViewerResumeInactive || !pViewerPlayer.PPlayerReady)
        {
            pViewerResumeInactive = false;
            if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
            {
                pViewerClockTimer.Start();
            }
            return;
        }

        pViewerResumeInactive = false;
        pViewerPlayer.PPlayerPlay();
        PViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Start();
    }

    private void PViewerMediaRaise(LCargo mediaStatus)
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
        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        TimeSpan playbackPosition = pViewerPlayer.PPlayerTimeRead();
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

    private void PPlayerStopDispose()
    {
        pViewerClockTimer.Stop();
        pPlayerAccurateActive = false;
        pViewerResumeInactive = false;
        PViewerPlaybackUpdate(false, null);
        Player? pPlayerPrevious = pViewerPlayer.PPlayerFlyleafPlayer;
        if (pPlayerPrevious is not null)
        {
            pPlayerPrevious.SeekCompleted -= PPlayerSeekHandle;
        }

        if (pViewerFlyleafHost is not null) pViewerFlyleafHost.Player = null;
        LTraceLog.LTraceInfoRecord($"Viewer host detached: player {(pPlayerPrevious is null ? "none" : "released")}");
        pViewerPlayer.PPlayerDispose();
    }
}

using System;
using Cadroue.Media;
using FlyleafLib;
using FlyleafLib.MediaPlayer;

using Cadroue.Core;

using Cadroue.Application;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    private void PPlayerVideoLoad(PViewerIntent pViewerRequest)
    {
        if (!pViewerCommandActive) return;
        PViewerEngineSelect();
        int loadSerial = ++pViewerLoadSerial;
        pViewerClockTimer.Stop();
        pViewerResumeInactive = false;
        if (pViewerPlayer.PPlayerReady)
        {
            pViewerPlayer.PPlayerPause();
            PViewerPlaybackUpdate(false, null);
        }

        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            return;
        }

        string sourcePath = pViewerRequest.PViewerIntentPath;
        string pViewerLoadPath;
        try
        {
            pViewerLoadPath = System.IO.Path.GetFullPath(sourcePath);
        }
        catch (Exception pViewerPathException) when (pViewerPathException is ArgumentException or NotSupportedException)
        {
            pViewerLoadPath = sourcePath;
        }

        pViewerIntent = pViewerRequest with { PViewerIntentPath = pViewerLoadPath };
        pViewerMediaProbe.LMediaLoadTail = PViewerEngineRead() != LPreviewEngine.LPreviewEngineMpv;
        LTraceLog.LTraceInfoRecord(
            $"Video load start serial={loadSerial} '{System.IO.Path.GetFileName(sourcePath)}'",
            $"engine={PViewerEngineCurrent}, probing (ffprobe)…");
        _ = pViewerMediaProbe.LMediaLoadStart(sourcePath);
    }

    private void PViewerLoadHandle(LMediaLoadOutcome result)
    {
        int loadSerial = pViewerLoadSerial;
        LTraceLog.LTraceInfoRecord(
            $"Video probe outcome {result.LMediaLoadKind} '{System.IO.Path.GetFileName(result.LMediaLoadPath)}'",
            result.LMediaLoadError);
        Dispatcher.BeginInvoke(() =>
        {
            if (pViewerUnloaded
                || loadSerial != pViewerLoadSerial
                || pViewerIntent is not { } pViewerPending
                || !string.Equals(
                    result.LMediaLoadPath, pViewerPending.PViewerIntentPath, StringComparison.OrdinalIgnoreCase))
            {
                LTraceLog.LTraceInfoRecord(
                    "Video probe outcome discarded (stale/superseded): "
                    + $"serial got={loadSerial} now={pViewerLoadSerial}, unloaded={pViewerUnloaded}");
                return;
            }

            if (result.LMediaLoadKind == LMediaLoadKind.LMediaLoadSuccess)
            {
                PPlayerMediaApply(result.LMediaLoadPath, result.LMediaLoadInfo, null, loadSerial);
                return;
            }

            if (result.LMediaLoadKind == LMediaLoadKind.LMediaLoadFailure)
            {
                PViewerCargoCommit(new LCargo(
                    result.LMediaLoadPath,
                    null,
                    false,
                    false,
                    result.LMediaLoadError,
                    null));
            }
        });
    }

    private void PViewerCargoCommit(LCargo pViewerStatus)
    {
        if (pViewerMpvActive)
        {
            PViewerMpvCommit(pViewerStatus);
            return;
        }

        PViewerMediaCommit(pViewerStatus, null);
    }

    private async void PPlayerMediaApply(string sourcePath, LMediaInfo? mediaInfo, string? ffmpegError, int loadSerial)
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
        bool pPlayerCreated = player is null;
        string? previewError = null;
        var pPlayerClock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            player ??= PPlayerFlyleafCreate(pPlayerClock);
            player.Audio.Volume = (int)Math.Round(pViewerVolume);
            Player pPlayerOpening = player;
            double pPlayerBeforeOpen = pPlayerClock.Elapsed.TotalMilliseconds;
            await System.Threading.Tasks.Task.Run(() => PPlayerFlyleafOpen(pPlayerOpening, sourcePath));
            LTrace.LTraceRecord(
                LTraceKind.LTraceUi,
                $"Player opened '{System.IO.Path.GetFileName(sourcePath)}'",
                PPlayerAccelRead(player),
                pPlayerClock.Elapsed.TotalMilliseconds - pPlayerBeforeOpen);
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
            if (pPlayerCreated)
            {
                PPlayerFlyleafDispose(player);
            }

            LTraceLog.LTraceInfoRecord(
                "Player open discarded (stale/superseded): "
                + $"serial got={loadSerial} now={pViewerLoadSerial}, unloaded={pViewerUnloaded}");
            return;
        }

        if (player is not null)
        {
            pPlayerRendererPending = true;
            PPlayerStartPause(player);
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
        PViewerFlyleafApply();
        pViewerMediaInfo = mediaStatus.LCargoMediaInfo;
        pViewerPlayer.PPlayerEndSet(pViewerMediaInfo?.LMediaVideoEnd);
        PViewerSourcePath = mediaStatus.LCargoSourcePath;
        LPreviewStateCurrent = LPreviewStateCurrent.LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        pViewerEndReached = false;
        if (!PCropPersistent)
        {
            PCropVideo = null;
            LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(null);
            PCropHide();
        }

        PViewerIntent? pViewerRequest = pViewerIntent;
        pViewerIntent = null;
        PViewerMediaRaise(mediaStatus);
        if (player is null)
        {
            PViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        PViewerPreviewRestore();
        PViewerIntentApply(pViewerRequest);
    }

    private void PViewerIntentApply(PViewerIntent? pViewerRequest)
    {
        TimeSpan pViewerPosition = pViewerRequest?.PViewerIntentPosition ?? TimeSpan.Zero;
        if (pViewerPosition > TimeSpan.Zero)
        {
            PPlayerAccurateSeek(pViewerPosition);
        }

        bool pViewerPlaying = pViewerRequest?.PViewerIntentPlaying
            ?? LPreference.LPreferenceStateCurrent.LPreferenceAutoplay;
        pViewerResumeInactive = false;
        if (!pViewerPlaying)
        {
            pViewerPlayer.PPlayerPause();
            PViewerPlaybackUpdate(false, pViewerPosition);
            return;
        }

        pViewerPlayer.PPlayerPlay();
        PViewerPlaybackUpdate(true, pViewerPosition);
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
}

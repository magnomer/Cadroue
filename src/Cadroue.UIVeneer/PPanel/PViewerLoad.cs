using System;
using Cadroue.Media;
using FlyleafLib;
using FlyleafLib.MediaPlayer;

using Cadroue.Core;

using Cadroue.Application;

using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PViewer
{
    private void PPlayerVideoLoad(LViewerIntent pViewerRequest)
    {
        if (!LViewer.LViewerCommandActive) return;
        PViewerEngineSelect();
        int loadSerial = LViewer.LViewerSerialChange();
        pViewerClockTimer.Stop();
        LViewer.LViewerResumeSet(false);
        if (pViewerPlayer.PPlayerReady)
        {
            pViewerPlayer.PPlayerPause();
            LViewer.LViewerPlaybackUpdate(false, null);
        }

        if (!LViewer.LViewerSerialCheck(loadSerial))
        {
            return;
        }

        string sourcePath = pViewerRequest.LViewerIntentPath;
        string pViewerLoadPath;
        try
        {
            pViewerLoadPath = System.IO.Path.GetFullPath(sourcePath);
        }
        catch (Exception pViewerPathException) when (pViewerPathException is ArgumentException or NotSupportedException)
        {
            pViewerLoadPath = sourcePath;
        }

        LViewer.LViewerIntentSet(pViewerRequest with { LViewerIntentPath = pViewerLoadPath });
        pViewerMediaProbe.LMediaLoadTail = PViewerEngineRead() != LPreviewEngine.LPreviewEngineMpv;
        LTraceLog.LTraceInfoRecord(
            $"Video load start serial={loadSerial} '{System.IO.Path.GetFileName(sourcePath)}'",
            $"engine={PViewerEngineCurrent}, probing (ffprobe)…");
        _ = pViewerMediaProbe.LMediaLoadStart(sourcePath);
    }

    private void PViewerLoadHandle(LMediaLoadOutcome result)
    {
        int loadSerial = LViewer.LViewerLoadSerial;
        LTraceLog.LTraceInfoRecord(
            $"Video probe outcome {result.LMediaLoadKind} '{System.IO.Path.GetFileName(result.LMediaLoadPath)}'",
            result.LMediaLoadError);
        Dispatcher.BeginInvoke(() =>
        {
            if (LViewer.LViewerUnloaded
                || loadSerial != LViewer.LViewerLoadSerial
                || LViewer.LViewerIntent is not { } pViewerPending
                || !string.Equals(
                    result.LMediaLoadPath, pViewerPending.LViewerIntentPath, StringComparison.OrdinalIgnoreCase))
            {
                LTraceLog.LTraceInfoRecord(
                    "Video probe outcome discarded (stale/superseded): "
                    + $"serial got={loadSerial} now={LViewer.LViewerLoadSerial}, unloaded={LViewer.LViewerUnloaded}");
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
        if (LViewer.LViewerMpvActive)
        {
            PViewerMpvCommit(pViewerStatus);
            return;
        }

        PViewerMediaCommit(pViewerStatus, null);
    }

    private async void PPlayerMediaApply(string sourcePath, LMediaInfo? mediaInfo, string? ffmpegError, int loadSerial)
    {
        if (LViewer.LViewerMpvActive)
        {
            PViewerMpvApply(sourcePath, mediaInfo, ffmpegError, loadSerial);
            return;
        }

        if (mediaInfo is { LMediaAudioOnly: true } && !LViewer.LViewerAudioAllowed)
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
            player.Audio.Volume = (int)Math.Round(LViewer.LViewerVolume);
            Player pPlayerOpening = player;
            double pPlayerBeforeOpen = pPlayerClock.Elapsed.TotalMilliseconds;
            await LPlayer.LPlayerOpenStart(sourcePath, pPath => PPlayerFlyleafOpen(pPlayerOpening, pPath));
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

        if (!LViewer.LViewerSerialCheck(loadSerial))
        {
            if (pPlayerCreated)
            {
                PPlayerFlyleafDispose(player);
            }

            LTraceLog.LTraceInfoRecord(
                "Player open discarded (stale/superseded): "
                + $"serial got={loadSerial} now={LViewer.LViewerLoadSerial}, unloaded={LViewer.LViewerUnloaded}");
            return;
        }

        if (player is not null)
        {
            LPlayer.LPlayerRendererSet(true);
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
        LPlayer.LPlayerAccurateReset();

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
        pViewerPlayer.PPlayerEndSet(mediaStatus.LCargoMediaInfo?.LMediaVideoEnd);
        LViewerIntent? pViewerRequest = PViewerCargoApply(mediaStatus);
        if (player is null)
        {
            LViewer.LViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        PViewerPreviewRestore();
        PViewerIntentApply(pViewerRequest);
    }

    private LViewerIntent? PViewerCargoApply(LCargo mediaStatus)
    {
        LViewer.LViewerMediaCommit(mediaStatus, PCropPersistent);
        if (!PCropPersistent)
        {
            PCropHide();
        }

        LViewerIntent? pViewerRequest = LViewer.LViewerIntent;
        LViewer.LViewerIntentSet(null);
        LViewer.LViewerMediaRaise(mediaStatus);
        return pViewerRequest;
    }

    private void PViewerIntentApply(LViewerIntent? pViewerRequest)
    {
        TimeSpan pViewerPosition = pViewerRequest?.LViewerIntentPosition ?? TimeSpan.Zero;
        if (pViewerPosition > TimeSpan.Zero)
        {
            PPlayerAccurateSeek(pViewerPosition);
        }

        bool pViewerPlaying = pViewerRequest?.LViewerIntentPlaying
            ?? LPreference.LPreferenceStateCurrent.LPreferenceAutoplay;
        LViewer.LViewerResumeSet(false);
        if (!pViewerPlaying)
        {
            pViewerPlayer.PPlayerPause();
            LViewer.LViewerPlaybackUpdate(false, pViewerPosition);
            return;
        }

        pViewerPlayer.PPlayerPlay();
        LViewer.LViewerPlaybackUpdate(true, pViewerPosition);
        pViewerClockTimer.Start();
    }
}

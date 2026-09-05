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
                || !string.Equals(result.LMediaLoadPath, pViewerLoadPath, StringComparison.OrdinalIgnoreCase))
            {
                LTraceLog.LTraceInfoRecord(
                    $"Video probe outcome discarded (stale/superseded): serial got={loadSerial} now={pViewerLoadSerial}, unloaded={pViewerUnloaded}");
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
                player.Config.Player.KeyBindings.Keys.Clear();
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

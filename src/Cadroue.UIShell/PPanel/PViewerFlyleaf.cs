using System;
using FlyleafLib;
using FlyleafLib.MediaPlayer;

using Cadroue.Application;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    private static readonly System.Windows.Media.Color PViewerBackColor = System.Windows.Media.Colors.White;

    public bool PViewerColorPreview { get; set; }

    private void PPlayerAccurateSeek(TimeSpan playbackPosition)
    {
        if (pViewerMpvActive)
        {
            PViewerSeekRecord(playbackPosition, "mpv engine seeks directly");
            pViewerPlayer.PPlayerSeek(playbackPosition);
            return;
        }

        bool pPlayerWasRunning = pPlayerAccurateActive;
        pPlayerAccurateActive = true;
        PViewerSeekRecord(
            playbackPosition,
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

    private static void PPlayerStartPause(Player player)
    {
        player.Pause();
        player.Seek(0);
    }

    private void PViewerFlyleafApply()
    {
        if (pViewerFlyleafHost is null)
        {
            return;
        }

        try
        {
            nint pViewerSurfaceHandle = PViewerWindowHandle(pViewerFlyleafHost.Surface);
            if (pViewerSurfaceHandle != nint.Zero)
            {
                PViewerInertApply(pViewerSurfaceHandle);
            }

            nint pViewerOverlayHandle = PViewerWindowHandle(pViewerFlyleafHost.Overlay);
            if (pViewerOverlayHandle != nint.Zero)
            {
                PViewerInertApply(pViewerOverlayHandle);
            }
        }
        catch
        {
        }
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

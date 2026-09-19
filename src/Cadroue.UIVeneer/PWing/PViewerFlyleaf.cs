using System;
using FlyleafLib;
using FlyleafLib.MediaFramework.MediaRenderer;
using FlyleafLib.MediaPlayer;

using Cadroue.Core;
using Cadroue.Application;

using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PViewer
{
    private static readonly System.Windows.Media.Color PViewerBackColor = System.Windows.Media.Colors.White;

    private void PPlayerAccurateSeek(TimeSpan playbackPosition)
    {
        if (LViewer.LViewerMpvActive)
        {
            LViewer.LViewerSeekRecord(playbackPosition, "mpv engine seeks directly");
            pViewerPlayer.PPlayerSeek(playbackPosition);
            return;
        }

        bool pPlayerWasRunning = LPlayer.LPlayerAccurateSet();
        LViewer.LViewerSeekRecord(
            playbackPosition,
            pPlayerWasRunning
                ? "a seek was still running; queued for Flyleaf to conflate"
                : "no seek was in flight");
        pViewerPlayer.PPlayerSeek(playbackPosition);
    }

    private void PPlayerSeekHandle(object? sender, int seekMilliseconds)
    {
        if (LPlayer.LPlayerSeekCommit(seekMilliseconds) && sender is Player pPlayerSeeked)
        {
            PPlayerFactsRecord(pPlayerSeeked, "Renderer resolved after the first completed seek");
        }
    }

    private Player PPlayerFlyleafCreate(System.Diagnostics.Stopwatch pPlayerClock)
    {
        var player = new Player(new Config());
        player.Config.Player.KeyBindings.Keys.Clear();
        if (LViewer.LViewerColorPreview && LFlyleaf.LFlyleafActive)
        {
            player.Config.Video.VideoProcessor = VideoProcessors.Flyleaf;
            player.Config.Video.SyncVPFilters = false;
        }

        player.Config.Video.BackColor = PViewerBackColor;
        player.Config.Video.ClearScreen = false;
        player.SeekCompleted += PPlayerSeekHandle;
        LRenderer.LRendererFactsRecord(
            PPlayerFactsRead(
                player,
                "Player created",
                $"max output {player.Config.Video.MaxOutputFps}fps, "
                + $"decoder threads {player.Config.Decoder.VideoThreads}, "
                + $"max video frames {player.Config.Decoder.MaxVideoFrames}, "
                + $"clear screen {player.Config.Video.ClearScreen}, sws forced {player.Config.Video.SwsForce}"),
            pPlayerClock.Elapsed.TotalMilliseconds);
        return player;
    }

    private static void PPlayerFlyleafOpen(Player player, string sourcePath)
    {
        var openResult = player.Open(sourcePath);
        if (!openResult.Success)
        {
            throw new InvalidOperationException(
                openResult.Error ?? LLocalization.LLocalizationTextRead("Viewer.Error.FlyleafOpen"));
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
        LPlayer.LPlayerAccurateReset();
        LViewer.LViewerResumeSet(false);
        LViewer.LViewerPlaybackUpdate(false, null);
        Player? pPlayerPrevious = pViewerPlayer.PPlayerFlyleafPlayer;
        if (pPlayerPrevious is not null)
        {
            pPlayerPrevious.SeekCompleted -= PPlayerSeekHandle;
        }

        if (pViewerFlyleafHost is not null) pViewerFlyleafHost.Player = null;
        LTraceLog.LTraceInfoRecord($"Viewer host detached: player {(pPlayerPrevious is null ? "none" : "released")}");
        pViewerPlayer.PPlayerDispose();
    }

    private static LRendererFacts PPlayerFactsRead(Player player, string reason, string? config = null)
    {
        player.Config.Video.FLFilters.TryGetValue(FLFilters.Contrast, out FLFilter? pContrastFilter);
        var pPlayerStream = player.decoder?.VideoDecoder?.VideoStream;
        return new LRendererFacts(
            reason,
            player.Config.Video.VideoProcessor.ToString(),
            player.Config.Video.VideoAcceleration.ToString(),
            player.Config.Video.SyncVPFilters)
        {
            LRendererProcessorActive = player.Renderer?.VideoProcessor.ToString(),
            LRendererAccelerated = player.decoder?.VideoDecoder?.VideoAccelerated,
            LRendererAdapter = player.Renderer?.GPUAdapter?.Description,
            LRendererPixel = pPlayerStream?.PixelFormatStr,
            LRendererColorRange = pPlayerStream?.ColorRange.ToString(),
            LRendererColorSpace = pPlayerStream?.ColorSpace.ToString(),
            LRendererContrast = pContrastFilter?.Value,
            LRendererConfig = config
        };
    }

    private static void PPlayerFactsRecord(Player? player, string reason)
    {
        if (player is null)
        {
            return;
        }

        LRenderer.LRendererFactsRecord(PPlayerFactsRead(player, reason));
    }
}

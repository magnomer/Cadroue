using System;
using System.Linq;
using System.Reflection;
using Cadroue.Media;
using FlyleafLib;
using FlyleafLib.MediaFramework.MediaRenderer;
using FlyleafLib.MediaPlayer;

using Cadroue.Core;

using Cadroue.Infrastructure;


namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
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
}

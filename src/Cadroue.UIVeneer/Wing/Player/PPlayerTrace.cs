using FlyleafLib;
using FlyleafLib.MediaFramework.MediaStream;
using FlyleafLib.MediaPlayer;

using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PPlayerTrace
{
    private readonly Player pPlayer;

    public PPlayerTrace(Player pPlayerEngine) => pPlayer = pPlayerEngine;

    public void PPlayerSetupRecord(QPlayer lPlayer) =>
        LRendererTrace.LRendererSetupRecord(
            PPlayerFactsRead(pPlayer.VideoDecoder.VideoStream),
            pPlayer.Config.Video.MaxOutputFps,
            pPlayer.Config.Decoder.VideoThreads,
            pPlayer.Config.Decoder.MaxVideoFrames,
            pPlayer.Config.Video.ClearScreen,
            pPlayer.Config.Video.SwsForce,
            lPlayer.QPlayerCreateRead());

    public void PPlayerFactsRecord(string pReason, double? pMilliseconds) =>
        LRendererTrace.LRendererFactsRecord(pReason, PPlayerFactsRead(pPlayer.VideoDecoder.VideoStream), pMilliseconds);

    private LRendererFacts PPlayerFactsRead(VideoStream? pPlayerStream) => new(
        pPlayer.Config.Video.VideoProcessor.ToString(),
        pPlayer.Config.Video.VideoAcceleration.ToString(),
        pPlayer.Config.Video.SyncVPFilters)
    {
        LRendererProcessorActive = pPlayer.Renderer?.VideoProcessor.ToString(),
        LRendererAccelerated = pPlayer.decoder?.VideoDecoder?.VideoAccelerated,
        LRendererAdapter = pPlayer.Renderer?.GPUAdapter?.Description,
        LRendererPixel = pPlayerStream?.PixelFormatStr,
        LRendererColorRange = pPlayerStream?.ColorRange.ToString(),
        LRendererColorSpace = pPlayerStream?.ColorSpace.ToString(),
        LRendererContrast = pPlayer.Config.Video.FLFilters.GetValueOrDefault(FLFilters.Contrast)?.Value
    };
}

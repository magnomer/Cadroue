using System;
using System.Collections.Generic;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaFramework.MediaRenderer;
using FlyleafLib.MediaFramework.MediaStream;
using FlyleafLib.MediaPlayer;

using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PPlayerFlyleaf
{
    private readonly Player pPlayer;
    private readonly FlyleafHost pPlayerHost;
    private readonly LPlayer lPlayer;

    public PPlayerFlyleaf(FlyleafHost pHost, LPlayer lPlayerOwner, Config pConfig)
    {
        lPlayerOwner.LPlayerCreateStart();
        pPlayerHost = pHost;
        lPlayer = lPlayerOwner;
        pPlayer = new Player(pConfig);
        PPlayerFilterAdd(FLFilters.Brightness, -100, 100, -0.5f, 0.5f);
        PPlayerFilterAdd(FLFilters.Contrast, -100, 100, 0f, 2f);
        PPlayerFilterAdd(FLFilters.Saturation, -100, 100, 0f, 2f);
        PPlayerFilterAdd(FLFilters.Hue, -180, 180, -3.14f, 3.14f);
        pPlayer.SeekCompleted += PPlayerSeekHandle;
        pPlayerHost.Player = pPlayer;
        LRenderer.LRendererSetupRecord(
            PPlayerFactsRead(pPlayer.VideoDecoder.VideoStream),
            pPlayer.Config.Video.MaxOutputFps,
            pPlayer.Config.Decoder.VideoThreads,
            pPlayer.Config.Decoder.MaxVideoFrames,
            pPlayer.Config.Video.ClearScreen,
            pPlayer.Config.Video.SwsForce,
            lPlayerOwner.LPlayerCreateRead());
    }

    public static nint PPlayerHandleRead(System.Windows.Window? pWindow)
    {
        try
        {
            return new System.Windows.Interop.WindowInteropHelper(pWindow!).Handle;
        }
        catch (ArgumentNullException)
        {
            return nint.Zero;
        }
    }

    public LPlayerSeam PPlayerSeamRead() => new(
        PPlayerOpen,
        pPlayer.Play,
        pPlayer.Pause,
        pPlayer.Stop,
        PPlayerSeek,
        PPlayerVolumeSet,
        null,
        null,
        PPlayerPreviewApply,
        null,
        PPlayerTimeRead,
        PPlayerEndedRead,
        PPlayerFactsRecord,
        PPlayerDispose);

    private void PPlayerOpen(string pSourcePath)
    {
        var pResult = pPlayer.Open(pSourcePath);
        pPlayerOpens[pResult.Success](pResult);
    }

    private static readonly IReadOnlyDictionary<bool, Action<OpenCompletedArgs>> pPlayerOpens =
        new Dictionary<bool, Action<OpenCompletedArgs>>
        {
            [true] = _ => { },
            [false] = pResult => throw new InvalidOperationException(LRenderer.LRendererErrorResolve(pResult.Error)),
        };

    private static readonly HashSet<Status> pPlayerEnded = [Status.Ended];

    private void PPlayerSeek(TimeSpan pPosition) => pPlayer.SeekAccurate((int)pPosition.TotalMilliseconds);

    private void PPlayerVolumeSet(double pVolume) => pPlayer.Audio.Volume = (int)Math.Round(pVolume);

    private TimeSpan PPlayerTimeRead() => TimeSpan.FromTicks(pPlayer.CurTime);

    private bool PPlayerEndedRead() => pPlayerEnded.Contains(pPlayer.Status);

    private void PPlayerSeekHandle(object? pSender, int pSeekMilliseconds) =>
        lPlayer.LPlayerSeekHandle(pSeekMilliseconds);

    private void PPlayerPreviewApply(LPreviewApplication pApplication)
    {
        PPlayerProcessorApply(pApplication.LPreviewReason);
        pPlayer.Config.Video.FLFilters[FLFilters.Brightness].Value = pApplication.LPreviewBrightness;
        pPlayer.Config.Video.FLFilters[FLFilters.Contrast].Value = pApplication.LPreviewContrast;
        pPlayer.Config.Video.FLFilters[FLFilters.Saturation].Value = pApplication.LPreviewSaturation;
        pPlayer.Config.Video.FLFilters[FLFilters.Hue].Value = pApplication.LPreviewHue;
        pPlayer.Config.Video.Rotation = pApplication.LPreviewRotation;
        pPlayer.Config.Video.HFlip = pApplication.LPreviewFlipHorizontal;
        pPlayer.Config.Video.VFlip = pApplication.LPreviewFlipVertical;
    }

    private void PPlayerProcessorApply(string pReason)
    {
        VideoProcessors pWas = pPlayer.Config.Video.VideoProcessor;
        pPlayer.Config.Video.VideoProcessor = VideoProcessors.Flyleaf;
        LRenderer.LRendererProcessorRecord(pWas.ToString(), pReason);
    }

    private void PPlayerFilterAdd(
        FLFilters pFilter, int pMinimum, int pMaximum, float pMinimumShader, float pMaximumShader) =>
        pPlayer.Config.Video.FLFilters.TryAdd(pFilter, new FLFilter
        {
            Filter = pFilter,
            Minimum = pMinimum,
            Maximum = pMaximum,
            Default = 0,
            Value = 0,
            Step = 1,
            MinimumPS = pMinimumShader,
            MaximumPS = pMaximumShader
        });

    private void PPlayerFactsRecord(string pReason, double? pMilliseconds) =>
        LRenderer.LRendererFactsRecord(pReason, PPlayerFactsRead(pPlayer.VideoDecoder.VideoStream), pMilliseconds);

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

    private void PPlayerDispose()
    {
        pPlayer.SeekCompleted -= PPlayerSeekHandle;
        pPlayerHost.Player = null;
        try
        {
            pPlayer.Stop();
            pPlayer.Dispose();
        }
        catch
        {
        }
    }
}

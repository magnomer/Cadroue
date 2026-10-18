using FlyleafLib;
using FlyleafLib.MediaFramework.MediaRenderer;
using FlyleafLib.MediaPlayer;

using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer;

internal sealed class PPlayerPreview
{
    private readonly Player pPlayer;

    public PPlayerPreview(Player pPlayerEngine) => pPlayer = pPlayerEngine;

    public void PPlayerFilterPrepare()
    {
        PPlayerFilterAdd(FLFilters.Brightness, -100, 100, -0.5f, 0.5f);
        PPlayerFilterAdd(FLFilters.Contrast, -100, 100, 0f, 2f);
        PPlayerFilterAdd(FLFilters.Saturation, -100, 100, 0f, 2f);
        PPlayerFilterAdd(FLFilters.Hue, -180, 180, -3.14f, 3.14f);
    }

    public void PPlayerPreviewApply(LPreviewApplication pApplication)
    {
        PPlayerProcessorApply(pApplication.LPreviewReason);
        PPlayerVideoApply(pApplication);
    }

    private void PPlayerVideoApply(LPreviewApplication pApplication)
    {
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
        VideoProcessors pWas = PPlayerProcessorSet();
        LRendererTrace.LRendererProcessorRecord(pWas.ToString(), pReason);
    }

    private VideoProcessors PPlayerProcessorSet()
    {
        VideoProcessors pWas = pPlayer.Config.Video.VideoProcessor;
        pPlayer.Config.Video.VideoProcessor = VideoProcessors.Flyleaf;
        return pWas;
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
}

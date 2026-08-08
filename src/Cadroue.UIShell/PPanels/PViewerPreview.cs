using System;
using FlyleafLib;
using FlyleafLib.MediaFramework.MediaRenderer;
using FlyleafLib.MediaPlayer;

using Cadroue.Infrastructure;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    internal static void PViewerPlayerApply(object pViewerTarget, LPreviewApplication pViewerApplication)
    {
        if (pViewerTarget is not Player pViewerPlayer)
        {
            return;
        }

        PViewerProcessorApply(pViewerPlayer, pViewerApplication.LPreviewReason);
        PViewerFilterApply(pViewerPlayer, FLFilters.Brightness, pViewerApplication.LPreviewBrightness);
        PViewerFilterApply(
            pViewerPlayer,
            FLFilters.Contrast,
            LFlyleaf.LFlyleafActive ? pViewerApplication.LPreviewContrast : 0);
        PViewerFilterApply(pViewerPlayer, FLFilters.Saturation, pViewerApplication.LPreviewSaturation);
        PViewerFilterApply(pViewerPlayer, FLFilters.Hue, pViewerApplication.LPreviewHue);

        pViewerPlayer.Config.Video.Rotation = pViewerApplication.LPreviewRotation;
        pViewerPlayer.Config.Video.HFlip = pViewerApplication.LPreviewFlipHorizontal;
        pViewerPlayer.Config.Video.VFlip = pViewerApplication.LPreviewFlipVertical;
    }

    private static void PViewerProcessorApply(Player pViewerPlayer, string pViewerReason)
    {
        VideoProcessors pViewerWas = pViewerPlayer.Config.Video.VideoProcessor;
        pViewerPlayer.Config.Video.VideoProcessor = VideoProcessors.Flyleaf;
        if (pViewerWas == VideoProcessors.Flyleaf)
        {
            return;
        }

        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            $"Video processor forced from {pViewerWas} to Flyleaf",
            "FLVP runs custom pixel shaders per frame; D3D11VP would use the driver's video processor\n"
            + $"caused by: {pViewerReason}");
    }

    private static void PViewerFilterApply(Player pViewerPlayer, FLFilters pViewerFilter, int pViewerValue)
    {
        if (!pViewerPlayer.Config.Video.FLFilters.TryGetValue(pViewerFilter, out FLFilter? pViewerFlyleafFilter))
        {
            pViewerFlyleafFilter = PViewerFilterCreate(pViewerFilter);
            pViewerPlayer.Config.Video.FLFilters[pViewerFilter] = pViewerFlyleafFilter;
        }

        pViewerFlyleafFilter.Value = PViewerValueClamp(
            pViewerValue,
            pViewerFlyleafFilter.Minimum,
            pViewerFlyleafFilter.Maximum);
    }

    private static FLFilter PViewerFilterCreate(FLFilters pViewerFilter)
    {
        return pViewerFilter switch
        {
            FLFilters.Brightness => new FLFilter
            {
                Filter = pViewerFilter,
                Minimum = -100,
                Maximum = 100,
                Default = 0,
                Value = 0,
                Step = 1,
                MinimumPS = -0.5f,
                MaximumPS = 0.5f
            },
            FLFilters.Contrast => new FLFilter
            {
                Filter = pViewerFilter,
                Minimum = -100,
                Maximum = 100,
                Default = 0,
                Value = 0,
                Step = 1,
                MinimumPS = 0f,
                MaximumPS = 2f
            },
            FLFilters.Saturation => new FLFilter
            {
                Filter = pViewerFilter,
                Minimum = -100,
                Maximum = 100,
                Default = 0,
                Value = 0,
                Step = 1,
                MinimumPS = 0f,
                MaximumPS = 2f
            },
            FLFilters.Hue => new FLFilter
            {
                Filter = pViewerFilter,
                Minimum = -180,
                Maximum = 180,
                Default = 0,
                Value = 0,
                Step = 1,
                MinimumPS = -3.14f,
                MaximumPS = 3.14f
            },
            _ => throw new ArgumentOutOfRangeException(nameof(pViewerFilter), pViewerFilter, null)
        };
    }

    private static int PViewerValueClamp(double pViewerValue, int pViewerMinimum, int pViewerMaximum)
    {
        return Math.Clamp((int)Math.Round(pViewerValue), pViewerMinimum, pViewerMaximum);
    }
}

using System;
using FlyleafLib;
using FlyleafLib.MediaFramework.MediaRenderer;
using FlyleafLib.MediaPlayer;

namespace Cadroue.UIShell.PPanels;

public static class LPreview
{
    public static void LPreviewApply(Player? lPreviewPlayer, LPreviewState lPreviewState)
    {
        if (lPreviewPlayer is null)
        {
            return;
        }

        LPreviewProcessorApply(lPreviewPlayer, "preview color/geometry");
        LPreviewColorApply(lPreviewPlayer, lPreviewState.LColor);
        LPreviewRotateApply(lPreviewPlayer, lPreviewState.LRotateFlip);
    }

    public static void LPreviewRestore(Player? lPreviewPlayer, LPreviewState lPreviewState)
    {
        if (lPreviewPlayer is null)
        {
            return;
        }

        LPreviewProcessorApply(lPreviewPlayer, "preview restored");
        lPreviewPlayer.Config.Video.Rotation = 0u;
        lPreviewPlayer.Config.Video.HFlip = false;
        lPreviewPlayer.Config.Video.VFlip = false;
        LPreviewApply(lPreviewPlayer, lPreviewState);
    }

    private static void LPreviewColorApply(Player lPreviewPlayer, LColor lColor)
    {
        LPreviewFilterApply(
            lPreviewPlayer,
            FLFilters.Brightness,
            LPreviewValueClamp(lColor.LColorBrightness * 100, -100, 100));

        LPreviewFilterApply(
            lPreviewPlayer,
            FLFilters.Contrast,
            LFlyleaf.LFlyleafActive
                ? LPreviewValueClamp((lColor.LColorContrast - 1) * 100, -100, 100)
                : 0);

        LPreviewFilterApply(
            lPreviewPlayer,
            FLFilters.Saturation,
            LPreviewValueClamp((lColor.LColorSaturation - 1) * 100, -100, 100));

        LPreviewFilterApply(
            lPreviewPlayer,
            FLFilters.Hue,
            LPreviewValueClamp(lColor.LColorHue, -180, 180));
    }

    private static void LPreviewRotateApply(Player lPreviewPlayer, LRotateFlip lRotateFlip)
    {
        LPreviewProcessorApply(
            lPreviewPlayer,
            $"rotate {lRotateFlip.LRotateKind}, H {lRotateFlip.LRotateFlipHorizontal}, V {lRotateFlip.LRotateFlipVertical}");
        lPreviewPlayer.Config.Video.Rotation = LPreviewRotationRead(lRotateFlip.LRotateKind);
        lPreviewPlayer.Config.Video.HFlip = lRotateFlip.LRotateFlipHorizontal;
        lPreviewPlayer.Config.Video.VFlip = lRotateFlip.LRotateFlipVertical;
    }

    private static void LPreviewProcessorApply(Player lPreviewPlayer, string lPreviewReason)
    {
        VideoProcessors lPreviewWas = lPreviewPlayer.Config.Video.VideoProcessor;
        lPreviewPlayer.Config.Video.VideoProcessor = VideoProcessors.Flyleaf;
        if (lPreviewWas == VideoProcessors.Flyleaf)
        {
            return;
        }

        LTrace.LTraceRecord(
            LTraceKind.LTraceView,
            $"Video processor forced from {lPreviewWas} to Flyleaf",
            "FLVP runs custom pixel shaders per frame; D3D11VP would use the driver's video processor\n"
            + $"caused by: {lPreviewReason}");
    }

    private static void LPreviewFilterApply(Player lPreviewPlayer, FLFilters lPreviewFilter, int lPreviewValue)
    {
        if (!lPreviewPlayer.Config.Video.FLFilters.TryGetValue(lPreviewFilter, out FLFilter? lPreviewFlyleafFilter))
        {
            lPreviewFlyleafFilter = LPreviewFilterCreate(lPreviewFilter);
            lPreviewPlayer.Config.Video.FLFilters[lPreviewFilter] = lPreviewFlyleafFilter;
        }

        lPreviewFlyleafFilter.Value = LPreviewValueClamp(
            lPreviewValue,
            lPreviewFlyleafFilter.Minimum,
            lPreviewFlyleafFilter.Maximum);
    }

    private static FLFilter LPreviewFilterCreate(FLFilters lPreviewFilter)
    {
        return lPreviewFilter switch
        {
            FLFilters.Brightness => new FLFilter
            {
                Filter = lPreviewFilter,
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
                Filter = lPreviewFilter,
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
                Filter = lPreviewFilter,
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
                Filter = lPreviewFilter,
                Minimum = -180,
                Maximum = 180,
                Default = 0,
                Value = 0,
                Step = 1,
                MinimumPS = -3.14f,
                MaximumPS = 3.14f
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lPreviewFilter), lPreviewFilter, null)
        };
    }

    private static uint LPreviewRotationRead(LRotateKind lRotateKind)
    {
        return lRotateKind switch
        {
            LRotateKind.LRotate90 => 90u,
            LRotateKind.LRotate180 => 180u,
            LRotateKind.LRotate270 => 270u,
            _ => 0u
        };
    }

    private static int LPreviewValueClamp(double lPreviewValue, int lPreviewMinimum, int lPreviewMaximum)
    {
        return Math.Clamp((int)Math.Round(lPreviewValue), lPreviewMinimum, lPreviewMaximum);
    }
}

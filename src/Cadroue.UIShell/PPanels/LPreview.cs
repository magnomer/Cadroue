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

        LPreviewColorApply(lPreviewPlayer, lPreviewState.LColor);
        LPreviewRotateApply(lPreviewPlayer, lPreviewState.LRotateFlip);
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
            LPreviewValueClamp((lColor.LColorContrast - 1) * 100, -100, 100));

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
        lPreviewPlayer.Config.Video.VideoProcessor = VideoProcessors.Flyleaf;
        lPreviewPlayer.Config.Video.Rotation = LPreviewRotationRead(lRotateFlip.LRotateKind);
        lPreviewPlayer.Config.Video.HFlip = lRotateFlip.LRotateFlipHorizontal;
        lPreviewPlayer.Config.Video.VFlip = lRotateFlip.LRotateFlipVertical;
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

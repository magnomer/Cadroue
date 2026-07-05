using System;
using FlyleafLib;
using FlyleafLib.MediaFramework.MediaRenderer;
using FlyleafLib.MediaPlayer;

namespace Cadroue.UIShell.PPanels;

public static class LPreviewFlyleafApply
{
    public static void LPreviewApply(Player? lPreviewPlayer, LPreviewState lPreviewState)
    {
        if (lPreviewPlayer is null)
        {
            return;
        }

        LPreviewFlyleafColorApply(lPreviewPlayer, lPreviewState.LColorAdjust);
        LPreviewFlyleafRotateFlipApply(lPreviewPlayer, lPreviewState.LRotateFlip);
    }

    private static void LPreviewFlyleafColorApply(Player lPreviewPlayer, LColorAdjust lColorAdjust)
    {
        LPreviewFlyleafFilterValueApply(
            lPreviewPlayer,
            FLFilters.Brightness,
            LPreviewFlyleafValueClamp(lColorAdjust.LColorAdjustBrightness * 100, -100, 100));

        LPreviewFlyleafFilterValueApply(
            lPreviewPlayer,
            FLFilters.Contrast,
            LPreviewFlyleafValueClamp((lColorAdjust.LColorAdjustContrast - 1) * 100, -100, 100));

        LPreviewFlyleafFilterValueApply(
            lPreviewPlayer,
            FLFilters.Saturation,
            LPreviewFlyleafValueClamp((lColorAdjust.LColorAdjustSaturation - 1) * 100, -100, 100));

        LPreviewFlyleafFilterValueApply(
            lPreviewPlayer,
            FLFilters.Hue,
            LPreviewFlyleafValueClamp(lColorAdjust.LColorAdjustHue, -180, 180));
    }

    private static void LPreviewFlyleafRotateFlipApply(Player lPreviewPlayer, LRotateFlip lRotateFlip)
    {
        lPreviewPlayer.Config.Video.Rotation = LPreviewFlyleafRotationRead(lRotateFlip.LRotateKind);
        lPreviewPlayer.Config.Video.HFlip = lRotateFlip.LRotateFlipHorizontal;
        lPreviewPlayer.Config.Video.VFlip = lRotateFlip.LRotateFlipVertical;
    }

    private static void LPreviewFlyleafFilterValueApply(Player lPreviewPlayer, FLFilters lPreviewFilter, int lPreviewValue)
    {
        if (!lPreviewPlayer.Config.Video.FLFilters.TryGetValue(lPreviewFilter, out FLFilter? lPreviewFlyleafFilter))
        {
            lPreviewFlyleafFilter = LPreviewFlyleafFilterCreate(lPreviewFilter);
            lPreviewPlayer.Config.Video.FLFilters[lPreviewFilter] = lPreviewFlyleafFilter;
        }

        lPreviewFlyleafFilter.Value = LPreviewFlyleafValueClamp(
            lPreviewValue,
            lPreviewFlyleafFilter.Minimum,
            lPreviewFlyleafFilter.Maximum);
    }

    private static FLFilter LPreviewFlyleafFilterCreate(FLFilters lPreviewFilter)
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

    private static uint LPreviewFlyleafRotationRead(LRotateKind lRotateKind)
    {
        return lRotateKind switch
        {
            LRotateKind.LRotate90 => 90u,
            LRotateKind.LRotate180 => 180u,
            LRotateKind.LRotate270 => 270u,
            _ => 0u
        };
    }

    private static int LPreviewFlyleafValueClamp(double lPreviewValue, int lPreviewMinimum, int lPreviewMaximum)
    {
        return Math.Clamp((int)Math.Round(lPreviewValue), lPreviewMinimum, lPreviewMaximum);
    }
}

using System;
using System.Windows;

namespace Cadroue.UIShell.PPanels;

public sealed record LCropBox(double LCropBoxX, double LCropBoxY, double LCropBoxWidth, double LCropBoxHeight)
{
    public static LCropBox? LCropBoxFromRect(Rect? lCropBoxRect)
    {
        if (lCropBoxRect is null)
        {
            return null;
        }

        Rect lCropBoxRectValue = lCropBoxRect.Value;
        if (lCropBoxRectValue.Width <= 0 || lCropBoxRectValue.Height <= 0)
        {
            return null;
        }

        return new LCropBox(lCropBoxRectValue.X, lCropBoxRectValue.Y, lCropBoxRectValue.Width, lCropBoxRectValue.Height);
    }

    public Rect LCropBoxRectCreate()
    {
        return new Rect(LCropBoxX, LCropBoxY, LCropBoxWidth, LCropBoxHeight);
    }
}

public sealed record LColorAdjust(
    double LColorAdjustBrightness,
    double LColorAdjustContrast,
    double LColorAdjustSaturation,
    double LColorAdjustHue)
{
    public static LColorAdjust LColorAdjustDefaultCreate()
    {
        return new LColorAdjust(0, 1, 1, 0);
    }
}

public enum LRotateKind
{
    LRotateNone,
    LRotate90,
    LRotate180,
    LRotate270
}

public sealed record LRotateFlip(LRotateKind LRotateKind, bool LRotateFlipHorizontal, bool LRotateFlipVertical)
{
    public static LRotateFlip LRotateFlipDefaultCreate()
    {
        return new LRotateFlip(LRotateKind.LRotateNone, false, false);
    }
}

public sealed record LPlaybackState(bool LPlaybackStatePlaying, TimeSpan LPlaybackPosition)
{
    public static LPlaybackState LPlaybackStateStoppedCreate()
    {
        return new LPlaybackState(false, TimeSpan.Zero);
    }
}

public sealed record LPreviewState(
    LCropBox? LCropBox,
    LColorAdjust LColorAdjust,
    LRotateFlip LRotateFlip,
    LPlaybackState LPlaybackState)
{
    public static LPreviewState LPreviewStateDefaultCreate()
    {
        return new LPreviewState(
            null,
            LColorAdjust.LColorAdjustDefaultCreate(),
            LRotateFlip.LRotateFlipDefaultCreate(),
            LPlaybackState.LPlaybackStateStoppedCreate());
    }

    public LPreviewState LCropBoxChange(LCropBox? lCropBox)
    {
        return this with { LCropBox = lCropBox };
    }

    public LPreviewState LColorAdjustChange(LColorAdjust lColorAdjust)
    {
        return this with { LColorAdjust = lColorAdjust };
    }

    public LPreviewState LRotateFlipChange(LRotateFlip lRotateFlip)
    {
        return this with { LRotateFlip = lRotateFlip };
    }

    public LPreviewState LPlaybackStateChange(LPlaybackState lPlaybackState)
    {
        return this with { LPlaybackState = lPlaybackState };
    }
}

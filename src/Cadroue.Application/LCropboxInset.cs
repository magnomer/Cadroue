using Cadroue.Core;

namespace Cadroue.Application;

public static class LCropboxInset
{
    private const int LCropboxSpanMinimum = 2;

    public static LWorkCrop LCropboxEdgeNormalize(
        LWorkCrop lCropboxCrop,
        double lCropboxFrameWidth,
        double lCropboxFrameHeight)
    {
        int lCropboxLeft = LCropboxEdgeResolve(lCropboxCrop.LWorkCropLeft);
        int lCropboxTop = LCropboxEdgeResolve(lCropboxCrop.LWorkCropTop);
        int lCropboxRight = LCropboxEdgeResolve(lCropboxCrop.LWorkCropRight);
        int lCropboxBottom = LCropboxEdgeResolve(lCropboxCrop.LWorkCropBottom);

        if (lCropboxFrameWidth > 0 && lCropboxFrameHeight > 0)
        {
            (lCropboxLeft, lCropboxRight) = LCropboxSpanClamp(lCropboxLeft, lCropboxRight, lCropboxFrameWidth);
            (lCropboxTop, lCropboxBottom) = LCropboxSpanClamp(lCropboxTop, lCropboxBottom, lCropboxFrameHeight);
        }

        return lCropboxCrop with
        {
            LWorkCropLeft = lCropboxLeft,
            LWorkCropTop = lCropboxTop,
            LWorkCropRight = lCropboxRight,
            LWorkCropBottom = lCropboxBottom
        };
    }

    public static LCropbox? LCropboxRectResolve(
        LWorkCrop lCropboxCrop,
        double lCropboxFrameWidth,
        double lCropboxFrameHeight)
    {
        if (lCropboxFrameWidth <= 0 || lCropboxFrameHeight <= 0)
        {
            return null;
        }

        LWorkCrop lCropboxCanonical = LCropboxEdgeNormalize(lCropboxCrop, lCropboxFrameWidth, lCropboxFrameHeight);
        if (!lCropboxCanonical.LWorkEdgeActive)
        {
            return null;
        }

        double lCropboxWidth = lCropboxFrameWidth
            - lCropboxCanonical.LWorkCropLeft - lCropboxCanonical.LWorkCropRight;
        double lCropboxHeight = lCropboxFrameHeight
            - lCropboxCanonical.LWorkCropTop - lCropboxCanonical.LWorkCropBottom;
        return lCropboxWidth > 0 && lCropboxHeight > 0
            ? new LCropbox(
                lCropboxCanonical.LWorkCropLeft,
                lCropboxCanonical.LWorkCropTop,
                lCropboxWidth,
                lCropboxHeight)
            : null;
    }

    // The crop's edges as insets, with the side numbered left, top, right, bottom from 0 taking lCropboxEdge.
    public static LCropboxEdges LCropboxSideResolve(LWorkCrop lCropboxCrop, int lCropboxSide, double lCropboxEdge)
    {
        double[] lCropboxEdges =
        {
            lCropboxCrop.LWorkCropLeft, lCropboxCrop.LWorkCropTop, lCropboxCrop.LWorkCropRight,
            lCropboxCrop.LWorkCropBottom
        };
        lCropboxEdges[lCropboxSide] = lCropboxEdge;
        return new LCropboxEdges(lCropboxEdges[0], lCropboxEdges[1], lCropboxEdges[2], lCropboxEdges[3]);
    }

    // The crop with the insets as its edges, each rounded to a whole pixel and never below zero.
    public static LWorkCrop LCropboxInsetResolve(LWorkCrop lCropboxCrop, LCropboxEdges lCropboxInsets) =>
        lCropboxCrop with
        {
            LWorkCropLeft = LCropboxInsetNormalize(lCropboxInsets.LCropboxLeft),
            LWorkCropTop = LCropboxInsetNormalize(lCropboxInsets.LCropboxTop),
            LWorkCropRight = LCropboxInsetNormalize(lCropboxInsets.LCropboxRight),
            LWorkCropBottom = LCropboxInsetNormalize(lCropboxInsets.LCropboxBottom)
        };

    private static int LCropboxInsetNormalize(double lCropboxEdge) => (int)Math.Max(0, Math.Round(lCropboxEdge));

    private static int LCropboxEdgeResolve(int lCropboxEdge) =>
        lCropboxEdge <= 0 ? 0 : lCropboxEdge - (lCropboxEdge % 2);

    private static (int, int) LCropboxSpanClamp(int lCropboxNear, int lCropboxFar, double lCropboxSpan)
    {
        int lCropboxLimit = LCropboxEdgeResolve((int)Math.Floor(lCropboxSpan) - LCropboxSpanMinimum);
        if (lCropboxLimit <= 0)
        {
            return (0, 0);
        }

        int lCropboxNearClamped = Math.Min(lCropboxNear, lCropboxLimit);
        int lCropboxFarClamped = Math.Min(lCropboxFar, LCropboxEdgeResolve(lCropboxLimit - lCropboxNearClamped));
        return (lCropboxNearClamped, lCropboxFarClamped);
    }
}

using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LNeutral
{
    public static LNeutralPoint LNeutralPointResolve(
        double lNeutralClickX,
        double lNeutralClickY,
        double lNeutralDisplayX,
        double lNeutralDisplayY,
        double lNeutralDisplayWidth,
        double lNeutralDisplayHeight,
        double lNeutralShownX,
        double lNeutralShownY,
        double lNeutralShownWidth,
        double lNeutralShownHeight,
        LRotateKind lNeutralRotateKind,
        bool lNeutralFlipHorizontal,
        bool lNeutralFlipVertical,
        int lNeutralSourceWidth,
        int lNeutralSourceHeight)
    {
        if (lNeutralSourceWidth <= 0
            || lNeutralSourceHeight <= 0
            || lNeutralDisplayWidth <= 0
            || lNeutralDisplayHeight <= 0
            || lNeutralShownWidth <= 0
            || lNeutralShownHeight <= 0
            || lNeutralClickX < lNeutralDisplayX
            || lNeutralClickY < lNeutralDisplayY
            || lNeutralClickX >= lNeutralDisplayX + lNeutralDisplayWidth
            || lNeutralClickY >= lNeutralDisplayY + lNeutralDisplayHeight)
        {
            return new LNeutralPoint(false, 0, 0);
        }

        bool lNeutralQuarter =
            lNeutralRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270;
        int lNeutralRotatedWidth = lNeutralQuarter ? lNeutralSourceHeight : lNeutralSourceWidth;
        int lNeutralRotatedHeight = lNeutralQuarter ? lNeutralSourceWidth : lNeutralSourceHeight;

        double lNeutralFractionX =
            (lNeutralClickX - lNeutralDisplayX) / lNeutralDisplayWidth;
        double lNeutralFractionY =
            (lNeutralClickY - lNeutralDisplayY) / lNeutralDisplayHeight;

        double lNeutralFinalX = lNeutralShownX + (lNeutralFractionX * lNeutralShownWidth);
        double lNeutralFinalY = lNeutralShownY + (lNeutralFractionY * lNeutralShownHeight);

        int lNeutralFinalPixelX = Math.Clamp(
            (int)Math.Floor(lNeutralFinalX), 0, lNeutralRotatedWidth - 1);
        int lNeutralFinalPixelY = Math.Clamp(
            (int)Math.Floor(lNeutralFinalY), 0, lNeutralRotatedHeight - 1);

        (int lNeutralSourceX, int lNeutralSourceY) = lNeutralRotateKind switch
        {
            LRotateKind.LRotate90 => (
                lNeutralFinalPixelY,
                lNeutralSourceHeight - 1 - lNeutralFinalPixelX),
            LRotateKind.LRotate270 => (
                lNeutralSourceWidth - 1 - lNeutralFinalPixelY,
                lNeutralFinalPixelX),
            LRotateKind.LRotate180 => (
                lNeutralSourceWidth - 1 - lNeutralFinalPixelX,
                lNeutralSourceHeight - 1 - lNeutralFinalPixelY),
            _ => (lNeutralFinalPixelX, lNeutralFinalPixelY)
        };

        if (lNeutralFlipVertical)
        {
            lNeutralSourceY = lNeutralSourceHeight - 1 - lNeutralSourceY;
        }

        if (lNeutralFlipHorizontal)
        {
            lNeutralSourceX = lNeutralSourceWidth - 1 - lNeutralSourceX;
        }

        return new LNeutralPoint(true, lNeutralSourceX, lNeutralSourceY);
    }
}

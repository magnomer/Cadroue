using System;

namespace Cadroue.Core;

public sealed class LFrameState
{
    public double LFrameWidth { get; set; }
    public double LFrameHeight { get; set; }
    public double? LFrameLeft { get; set; }
    public double? LFrameTop { get; set; }
    public double LFrameFlowHeight { get; set; }

    public static LFrameState LFrameDefaultCreate()
    {
        return new LFrameState
        {
            LFrameWidth = 1280,
            LFrameHeight = 760,
            LFrameLeft = null,
            LFrameTop = null,
            LFrameFlowHeight = 280
        };
    }

    public void LFrameNormalize()
    {
        LFrameWidth = LFrameNumberClamp(LFrameWidth, 800, 4000, 1280);
        LFrameHeight = LFrameNumberClamp(LFrameHeight, 400, 3000, 760);
        LFrameFlowHeight = LFrameNumberClamp(LFrameFlowHeight, 200, 520, 280);
    }

    private static double LFrameNumberClamp(double lFrameValue, double lFrameMinimum, double lFrameMaximum, double lFrameFallback)
    {
        if (double.IsNaN(lFrameValue) || double.IsInfinity(lFrameValue))
        {
            return lFrameFallback;
        }

        return Math.Clamp(lFrameValue, lFrameMinimum, lFrameMaximum);
    }
}

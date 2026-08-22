namespace Cadroue.Application;

public enum LBridgeOutcome
{
    LBridgeOutcomeSmart,
    LBridgeOutcomeWhole,
    LBridgeOutcomeInvalid
}

public sealed record LBridgeSpan(TimeSpan LBridgeSpanOrigin, TimeSpan LBridgeSpanEnd);

public sealed record LBridgePlan(
    LBridgeOutcome LBridgeOutcome,
    LBridgeSpan LBridgeInterval,
    LBridgeSpan? LBridgeHead,
    LBridgeSpan? LBridgeMiddle,
    LBridgeSpan? LBridgeTail);

public sealed record LBridgeStream(
    string LBridgeCodec,
    string LBridgeProfile,
    string LBridgePixel,
    string LBridgeColorSpace,
    string LBridgeColorPrimaries,
    string LBridgeColorTransfer,
    string LBridgeColorRange,
    string LBridgeFramerate,
    long LBridgeBitrate);

public static partial class LBridge
{
    private static readonly TimeSpan LBridgeTolerance = TimeSpan.FromMilliseconds(1);

    public static LBridgePlan LBridgeRegionResolve(
        IReadOnlyList<TimeSpan> lBridgeKeyframes,
        TimeSpan lBridgeOrigin,
        TimeSpan lBridgeEnd)
    {
        LBridgeSpan lBridgeInterval = new(lBridgeOrigin, lBridgeEnd);

        if (lBridgeEnd <= lBridgeOrigin)
        {
            return new LBridgePlan(LBridgeOutcome.LBridgeOutcomeInvalid, lBridgeInterval, null, null, null);
        }

        bool lBridgeOriginKeyed = false;
        TimeSpan? lBridgeFirstAfter = null;
        TimeSpan? lBridgeLastWithin = null;

        foreach (TimeSpan lBridgeKeyframe in lBridgeKeyframes)
        {
            if (LBridgeMatch(lBridgeKeyframe, lBridgeOrigin))
            {
                lBridgeOriginKeyed = true;
            }
            else if (lBridgeKeyframe > lBridgeOrigin && lBridgeFirstAfter is null)
            {
                lBridgeFirstAfter = lBridgeKeyframe;
            }

            if (lBridgeKeyframe <= lBridgeEnd + LBridgeTolerance)
            {
                lBridgeLastWithin = lBridgeKeyframe;
            }
        }

        TimeSpan? lBridgeCopyOrigin = lBridgeOriginKeyed ? lBridgeOrigin : lBridgeFirstAfter;
        TimeSpan? lBridgeCopyEnd = lBridgeLastWithin;

        bool lBridgeCopyUsable =
            lBridgeCopyOrigin is TimeSpan lBridgeStart &&
            lBridgeCopyEnd is TimeSpan lBridgeStop &&
            lBridgeStart < lBridgeStop;

        if (!lBridgeCopyUsable)
        {
            return new LBridgePlan(LBridgeOutcome.LBridgeOutcomeWhole, lBridgeInterval, null, null, null);
        }

        TimeSpan lBridgeCopyStart = lBridgeCopyOrigin!.Value;
        TimeSpan lBridgeCopyStop = lBridgeCopyEnd!.Value > lBridgeEnd ? lBridgeEnd : lBridgeCopyEnd!.Value;

        LBridgeSpan? lBridgeHead = lBridgeOriginKeyed
            ? null
            : new LBridgeSpan(lBridgeOrigin, lBridgeCopyStart);

        LBridgeSpan lBridgeCopy = new(lBridgeCopyStart, lBridgeCopyStop);

        LBridgeSpan? lBridgeTail = lBridgeCopyStop < lBridgeEnd - LBridgeTolerance
            ? new LBridgeSpan(lBridgeCopyStop, lBridgeEnd)
            : null;

        return new LBridgePlan(LBridgeOutcome.LBridgeOutcomeSmart, lBridgeInterval, lBridgeHead, lBridgeCopy, lBridgeTail);
    }

    private static bool LBridgeMatch(TimeSpan lBridgeLeft, TimeSpan lBridgeRight) =>
        (lBridgeLeft - lBridgeRight).Duration() <= LBridgeTolerance;
}

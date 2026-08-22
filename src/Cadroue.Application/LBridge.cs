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

public static partial class LBridge
{
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
            if (lBridgeKeyframe == lBridgeOrigin)
            {
                lBridgeOriginKeyed = true;
            }

            if (lBridgeKeyframe > lBridgeOrigin && lBridgeFirstAfter is null)
            {
                lBridgeFirstAfter = lBridgeKeyframe;
            }

            if (lBridgeKeyframe <= lBridgeEnd)
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
        TimeSpan lBridgeCopyStop = lBridgeCopyEnd!.Value;

        LBridgeSpan? lBridgeHead = lBridgeOriginKeyed
            ? null
            : new LBridgeSpan(lBridgeOrigin, lBridgeCopyStart);

        LBridgeSpan lBridgeCopy = new(lBridgeCopyStart, lBridgeCopyStop);

        LBridgeSpan? lBridgeTail = lBridgeCopyStop < lBridgeEnd
            ? new LBridgeSpan(lBridgeCopyStop, lBridgeEnd)
            : null;

        return new LBridgePlan(LBridgeOutcome.LBridgeOutcomeSmart, lBridgeInterval, lBridgeHead, lBridgeCopy, lBridgeTail);
    }

    public static bool LBridgeAudioCheck(string lBridgeAudioCodec)
    {
        string lBridgeCodec = (lBridgeAudioCodec ?? string.Empty).Trim().ToLowerInvariant();
        return lBridgeCodec.StartsWith("pcm", StringComparison.Ordinal);
    }
}

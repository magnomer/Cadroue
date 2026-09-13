using Cadroue.Core;

namespace Cadroue.Application;

public enum LBridgeOutcome
{
    LBridgeOutcomeSmart,
    LBridgeOutcomeWhole,
    LBridgeOutcomeInvalid
}

public sealed record LBridgeSpan(
    TimeSpan LBridgeSpanOrigin,
    TimeSpan LBridgeSpanEnd,
    TimeSpan? LBridgeDecodeEnd = null);

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
    long LBridgeBitrate,
    string LBridgeTimeBase = "");

public static partial class LBridge
{
    private static readonly TimeSpan LBridgeTolerance = TimeSpan.FromMilliseconds(1);

    public static LBridgePlan LBridgeRegionResolve(
        IReadOnlyList<TimeSpan> lBridgeKeyframes,
        TimeSpan lBridgeOrigin,
        TimeSpan lBridgeEnd,
        bool lBridgeOpenEnd = false)
        => LBridgeRegionResolve(
            lBridgeKeyframes.Select(lTime => new LKeyframeEntry(lTime, lTime)).ToArray(),
            lBridgeOrigin,
            lBridgeEnd,
            lBridgeOpenEnd);

    public static LBridgePlan LBridgeRegionResolve(
        IReadOnlyList<LKeyframeEntry> lBridgeKeyframes,
        TimeSpan lBridgeOrigin,
        TimeSpan lBridgeEnd,
        bool lBridgeOpenEnd = false)
    {
        LBridgeSpan lBridgeInterval = new(lBridgeOrigin, lBridgeEnd);

        if (lBridgeEnd <= lBridgeOrigin)
        {
            return new LBridgePlan(LBridgeOutcome.LBridgeOutcomeInvalid, lBridgeInterval, null, null, null);
        }

        LKeyframeEntry? lBridgeOriginKeyframe = null;
        LKeyframeEntry? lBridgeFirstAfter = null;
        LKeyframeEntry? lBridgeLastWithin = null;

        foreach (LKeyframeEntry lBridgeKeyframe in lBridgeKeyframes)
        {
            TimeSpan lBridgePresentation = lBridgeKeyframe.LKeyframePresentationTime;
            if (LBridgeMatch(lBridgePresentation, lBridgeOrigin))
            {
                if (lBridgeOriginKeyframe is null
                    || (lBridgePresentation - lBridgeOrigin).Duration()
                        < (lBridgeOriginKeyframe.LKeyframePresentationTime - lBridgeOrigin).Duration())
                {
                    lBridgeOriginKeyframe = lBridgeKeyframe;
                }
            }
            else if (lBridgePresentation > lBridgeOrigin && lBridgeFirstAfter is null)
            {
                lBridgeFirstAfter = lBridgeKeyframe;
            }

            if (lBridgePresentation <= lBridgeEnd + LBridgeTolerance)
            {
                lBridgeLastWithin = lBridgeKeyframe;
            }
        }

        bool lBridgeOriginKeyed = lBridgeOriginKeyframe is not null;
        TimeSpan? lBridgeCopyOrigin = lBridgeOriginKeyframe is { } lBridgeMatchedOrigin
            ? lBridgeMatchedOrigin.LKeyframePresentationTime
            : lBridgeFirstAfter?.LKeyframePresentationTime;
        TimeSpan? lBridgeCopyEnd = lBridgeLastWithin?.LKeyframePresentationTime;

        bool lBridgeCopyToEnd = lBridgeOpenEnd
            && lBridgeCopyOrigin is TimeSpan lBridgeOpenStart
            && lBridgeOpenStart < lBridgeEnd;

        bool lBridgeCopyUsable = lBridgeCopyToEnd ||
            (lBridgeCopyOrigin is TimeSpan lBridgeStart &&
             lBridgeCopyEnd is TimeSpan lBridgeStop &&
             lBridgeStart < lBridgeStop);

        if (!lBridgeCopyUsable)
        {
            return new LBridgePlan(LBridgeOutcome.LBridgeOutcomeWhole, lBridgeInterval, null, null, null);
        }

        TimeSpan lBridgeCopyStart = lBridgeCopyOrigin!.Value;
        TimeSpan lBridgeCopyStop = lBridgeCopyToEnd
            ? lBridgeEnd
            : lBridgeCopyEnd!.Value > lBridgeEnd ? lBridgeEnd : lBridgeCopyEnd!.Value;

        LBridgeSpan? lBridgeHead = lBridgeOriginKeyed
            ? null
            : new LBridgeSpan(lBridgeOrigin, lBridgeCopyStart);

        TimeSpan? lBridgeDecodeEnd = lBridgeCopyToEnd ? null : lBridgeLastWithin?.LKeyframeDecodeTime;
        if (lBridgeDecodeEnd is TimeSpan lBridgeDecodeStop
            && lBridgeDecodeStop <= lBridgeCopyStart + LBridgeTolerance)
        {
            lBridgeDecodeEnd = null;
        }

        LBridgeSpan lBridgeCopy = new(lBridgeCopyStart, lBridgeCopyStop, lBridgeDecodeEnd);

        LBridgeSpan? lBridgeTail = !lBridgeCopyToEnd && lBridgeCopyStop < lBridgeEnd - LBridgeTolerance
            ? new LBridgeSpan(lBridgeCopyStop, lBridgeEnd)
            : null;

        return new LBridgePlan(
            LBridgeOutcome.LBridgeOutcomeSmart,
            lBridgeInterval,
            lBridgeHead,
            lBridgeCopy,
            lBridgeTail);
    }

    public static bool LBridgeEndCheck(TimeSpan lBridgeEnd, TimeSpan lBridgeDuration, double lBridgeFramerate)
    {
        if (lBridgeDuration <= TimeSpan.Zero)
        {
            return false;
        }

        TimeSpan lBridgeFrame = lBridgeFramerate > 0
            ? TimeSpan.FromSeconds(1 / lBridgeFramerate)
            : LBridgeTolerance;
        return lBridgeEnd >= lBridgeDuration - lBridgeFrame;
    }

    private static bool LBridgeMatch(TimeSpan lBridgeLeft, TimeSpan lBridgeRight) =>
        (lBridgeLeft - lBridgeRight).Duration() <= LBridgeTolerance;
}

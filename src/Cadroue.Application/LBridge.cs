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
            // Callers that only have presentation timestamps describe a decode-order
            // stream with no reordering. Packet-based callers must supply real DTS.
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
            // UI and sidecar boundaries are millisecond-based. Seeking to that
            // rounded value can land just after the packet and, with
            // -copypriorss 0, discard the complete first GOP. Use the precise
            // packet PTS that established the keyframe match.
            ? lBridgeMatchedOrigin.LKeyframePresentationTime
            : lBridgeFirstAfter?.LKeyframePresentationTime;
        TimeSpan? lBridgeCopyEnd = lBridgeLastWithin?.LKeyframePresentationTime;

        // When the requested end reaches the source end, the region from the copy
        // start through EOF is one or more whole GOPs and is fully copyable: the final
        // GOP ends at a natural boundary, so it needs no re-encoded tail. Copy straight
        // through to the end. Only a genuine mid-stream end requires a tail bridge, and
        // reaching-the-end alone makes a lone-keyframe interval copyable, not a whole
        // re-encode.
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

        // A copy that runs to EOF has no following keyframe to stop before, so it needs
        // no decode-time cutoff.
        TimeSpan? lBridgeDecodeEnd = lBridgeCopyToEnd ? null : lBridgeLastWithin?.LKeyframeDecodeTime;
        if (lBridgeDecodeEnd is TimeSpan lBridgeDecodeStop
            && lBridgeDecodeStop <= lBridgeCopyStart + LBridgeTolerance)
        {
            // DTS is an optional precision hint for stopping before the tail GOP.
            // A missing or malformed hint must not erase a presentation-time middle.
            lBridgeDecodeEnd = null;
        }

        LBridgeSpan lBridgeCopy = new(lBridgeCopyStart, lBridgeCopyStop, lBridgeDecodeEnd);

        LBridgeSpan? lBridgeTail = !lBridgeCopyToEnd && lBridgeCopyStop < lBridgeEnd - LBridgeTolerance
            ? new LBridgeSpan(lBridgeCopyStop, lBridgeEnd)
            : null;

        return new LBridgePlan(LBridgeOutcome.LBridgeOutcomeSmart, lBridgeInterval, lBridgeHead, lBridgeCopy, lBridgeTail);
    }

    public static bool LBridgeEndCheck(TimeSpan lBridgeEnd, TimeSpan lBridgeDuration, double lBridgeFramerate)
    {
        if (lBridgeDuration <= TimeSpan.Zero)
        {
            return false;
        }

        // A cut that stops within one frame of the source end reaches it: the requested
        // and source ends arrive rounded to milliseconds, and a trim of a genuine frame
        // or more stays a real mid-stream end.
        TimeSpan lBridgeFrame = lBridgeFramerate > 0
            ? TimeSpan.FromSeconds(1 / lBridgeFramerate)
            : LBridgeTolerance;
        return lBridgeEnd >= lBridgeDuration - lBridgeFrame;
    }

    private static bool LBridgeMatch(TimeSpan lBridgeLeft, TimeSpan lBridgeRight) =>
        (lBridgeLeft - lBridgeRight).Duration() <= LBridgeTolerance;
}

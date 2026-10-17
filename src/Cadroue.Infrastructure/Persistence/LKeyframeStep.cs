using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal static class LKeyframeStep
{
    internal static LKeyframeMoveResult LKeyframeStepFind(
        IReadOnlyCollection<long> keyframes,
        IReadOnlySet<int> scannedSpans,
        TimeSpan duration,
        TimeSpan cursor,
        int direction,
        IReadOnlySet<int>? failedSpans = null,
        LKeyframeKind kind = LKeyframeKind.LKeyframeKindInter,
        double rate = 0)
    {
        if (kind != LKeyframeKind.LKeyframeKindInter)
        {
            return LKeyframeMoveResult.LKeyframeReadyCreate(
                LKeyframeStepResolve(duration, cursor, direction, kind, rate));
        }

        long durationMs = Math.Max(0, (long)Math.Ceiling(duration.TotalMilliseconds));
        long cursorMs = Math.Clamp((long)Math.Round(cursor.TotalMilliseconds), 0, durationMs);
        long rangeStartMs = direction < 0 ? 0 : cursorMs;
        long rangeEndMs = direction < 0 ? cursorMs : durationMs;

        if (rangeEndMs <= rangeStartMs)
        {
            return LKeyframeMoveResult.LKeyframeReadyCreate(null);
        }

        long? target = direction < 0
            ? keyframes.Where(ms => ms >= rangeStartMs && ms < cursorMs).Select(ms => (long?)ms).Max()
            : keyframes.Where(ms => ms > cursorMs && ms < rangeEndMs).Select(ms => (long?)ms).Min();

        long coverageStartMs = direction < 0 ? target ?? rangeStartMs : cursorMs;
        long coverageEndMs = direction < 0 ? cursorMs : target is null ? rangeEndMs : target.Value + 1;
        int firstSpan = (int)(coverageStartMs / LKeyframeSession.LKeyframeGridMilliseconds);
        int lastSpan = (int)((coverageEndMs - 1) / LKeyframeSession.LKeyframeGridMilliseconds);
        bool pending = false;
        for (int span = firstSpan; span <= lastSpan; span++)
        {
            if (failedSpans?.Contains(span) == true)
            {
                return LKeyframeMoveResult.LKeyframeFailedCreate();
            }

            pending |= !scannedSpans.Contains(span);
        }

        return pending
            ? LKeyframeMoveResult.LKeyframePending
            : LKeyframeMoveResult.LKeyframeReadyCreate(
                target is null ? null : TimeSpan.FromMilliseconds(target.Value));
    }

    private static TimeSpan? LKeyframeStepResolve(
        TimeSpan duration, TimeSpan cursor, int direction, LKeyframeKind kind, double rate)
    {
        TimeSpan frame = LKeyframeCodec.LKeyframeFrameResolve(rate);
        if (kind != LKeyframeKind.LKeyframeKindIntra || frame <= TimeSpan.Zero)
        {
            return null;
        }

        TimeSpan target = direction < 0 ? cursor - frame : cursor + frame;
        if (target < TimeSpan.Zero || target > duration)
        {
            return null;
        }

        return target;
    }
}

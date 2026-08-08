using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LKeyframeOrchestrator : IDisposable
{
    private const int LKeyframeGridMilliseconds = 20000;
    private readonly object lKeyframeLock = new();
    private readonly SortedSet<long> lKeyframeStorage = new();
    private readonly HashSet<int> lKeyframeScannedSpans = new();
    private const int LKeyframeRetryLimit = 3;

    private const int LKeyframeSaveCount = 10;

    private int lKeyframeUnsavedCount;
    private (int Keyframes, int Spans) lKeyframeSavedSignature = (-1, -1);
    private readonly Dictionary<int, int> lKeyframeFailedCounts = new();
    private CancellationTokenSource? lKeyframeCancelSource;
    private LKeyframeSourceIdentity? lKeyframeSourceIdentity;
    private string? lKeyframeSourcePath;
    private TimeSpan lKeyframeDuration;
    private int lKeyframeRequestSerial;
    private bool lKeyframeDisposed;

    public event Action<LKeyframeNotice>? LKeyframeNoticeReady;

    public int LKeyframeCurrentSerial => lKeyframeRequestSerial;

    public static TimeSpan LKeyframeSearchDuration => LKeyframeView.LKeyframeRangeBefore + LKeyframeView.LKeyframeRangeAfter;

    public void LKeyframeStart(string sourcePath, TimeSpan duration, TimeSpan cursor)
    {
        if (lKeyframeDisposed || string.IsNullOrWhiteSpace(sourcePath) || duration <= TimeSpan.Zero)
        {
            return;
        }

        CancellationTokenSource cancel;
        int serial;
        LKeyframeSourceIdentity identity;
        lock (lKeyframeLock)
        {
            if (LKeyframeSourceCheck(sourcePath, duration))
            {
                try
                {
                    identity = LKeyframeSourceIdentity.LKeyframeIdentityCreate(sourcePath, duration);
                }
                catch
                {
                    return;
                }

                lKeyframeStorage.Clear();
                lKeyframeScannedSpans.Clear();
                lKeyframeFailedCounts.Clear();
                lKeyframeSavedSignature = (-1, -1);
                lKeyframeSourceIdentity = identity;
                lKeyframeSourcePath = identity.LKeyframeSourcePath;
                lKeyframeDuration = duration;
                LKeyframeCacheLoad(identity);
            }
            else
            {
                identity = lKeyframeSourceIdentity!;
            }

            lKeyframeCancelSource?.Cancel();
            lKeyframeCancelSource?.Dispose();
            lKeyframeCancelSource = new CancellationTokenSource();
            cancel = lKeyframeCancelSource;
            serial = ++lKeyframeRequestSerial;
        }

        LKeyframeNoticePublish(serial);
        LKeyframePlanStart(identity.LKeyframeSourcePath, duration, cursor, serial, cancel.Token);
    }

    public void LKeyframeSuspend()
    {
        CancellationTokenSource? lKeyframeCancelPrevious;
        lock (lKeyframeLock)
        {
            lKeyframeCancelPrevious = lKeyframeCancelSource;
            lKeyframeCancelSource = null;
        }

        lKeyframeCancelPrevious?.Cancel();
        lKeyframeCancelPrevious?.Dispose();
    }

    public LKeyframeMoveResult LKeyframePreviousMove(TimeSpan cursor)
        => LKeyframeMoveFind(cursor, -1);

    public LKeyframeMoveResult LKeyframeNextMove(TimeSpan cursor)
        => LKeyframeMoveFind(cursor, 1);

    public LKeyframeMoveResult LKeyframeNearestMove(TimeSpan cursor)
    {
        var previous = LKeyframePreviousMove(cursor);
        var next = LKeyframeNextMove(cursor);
        if (!previous.LKeyframeReady || !next.LKeyframeReady)
        {
            return LKeyframeMoveResult.LKeyframePending;
        }

        if (previous.LKeyframeTarget is null) return next;
        if (next.LKeyframeTarget is null) return previous;
        return cursor - previous.LKeyframeTarget.Value <= next.LKeyframeTarget.Value - cursor ? previous : next;
    }

    private LKeyframeMoveResult LKeyframeMoveFind(TimeSpan cursor, int direction)
    {
        lock (lKeyframeLock)
        {
            return LKeyframeMoveResolve(
                lKeyframeStorage,
                lKeyframeScannedSpans,
                lKeyframeDuration,
                cursor,
                direction);
        }
    }

    internal static LKeyframeMoveResult LKeyframeMoveResolve(
        IReadOnlyCollection<long> keyframes,
        IReadOnlySet<int> scannedSpans,
        TimeSpan duration,
        TimeSpan cursor,
        int direction)
    {
        long durationMs = Math.Max(0, (long)Math.Ceiling(duration.TotalMilliseconds));
        long cursorMs = Math.Clamp((long)Math.Round(cursor.TotalMilliseconds), 0, durationMs);
        long searchRangeMs = (long)(direction < 0 ? LKeyframeView.LKeyframeRangeBefore : LKeyframeView.LKeyframeRangeAfter).TotalMilliseconds;
        long rangeStartMs = direction < 0 ? Math.Max(0, cursorMs - searchRangeMs) : cursorMs;
        long rangeEndMs = direction < 0 ? cursorMs : Math.Min(durationMs, cursorMs + searchRangeMs);

        if (rangeEndMs <= rangeStartMs)
        {
            return LKeyframeMoveResult.LKeyframeReadyResult(null);
        }

        long? target = direction < 0
            ? keyframes.Where(ms => ms >= rangeStartMs && ms < cursorMs).Select(ms => (long?)ms).Max()
            : keyframes.Where(ms => ms > cursorMs && ms < rangeEndMs).Select(ms => (long?)ms).Min();

        long coverageStartMs = direction < 0 ? target ?? rangeStartMs : cursorMs;
        long coverageEndMs = direction < 0 ? cursorMs : target is null ? rangeEndMs : target.Value + 1;
        int firstSpan = (int)(coverageStartMs / LKeyframeGridMilliseconds);
        int lastSpan = (int)((coverageEndMs - 1) / LKeyframeGridMilliseconds);
        for (int span = firstSpan; span <= lastSpan; span++)
        {
            if (!scannedSpans.Contains(span))
            {
                return LKeyframeMoveResult.LKeyframePending;
            }
        }

        return LKeyframeMoveResult.LKeyframeReadyResult(
            target is null ? null : TimeSpan.FromMilliseconds(target.Value));
    }

}

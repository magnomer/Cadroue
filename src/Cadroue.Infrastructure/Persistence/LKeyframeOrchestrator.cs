using Cadroue.Media;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LKeyframeOrchestrator : IDisposable
{
    private sealed record LKeyframeSignature(int LKeyframeSignatureCount, int LKeyframeSignatureSpans);

    private const int LKeyframeGridMilliseconds = 20000;
    private readonly object lKeyframeLock = new();
    private readonly object lKeyframeDispatchGate = new();
    private readonly SortedSet<long> lKeyframeStorage = new();
    private readonly HashSet<int> lKeyframeScannedSpans = new();
    private const int LKeyframeRetryLimit = 3;
    private static readonly TimeSpan LKeyframeScanLimit = TimeSpan.FromMinutes(1);

    private const int LKeyframeSaveCount = 10;

    private int lKeyframeUnsavedCount;
    private LKeyframeSignature lKeyframeSavedSignature = new(-1, -1);
    private readonly Dictionary<int, int> lKeyframeFailedCounts = new();
    private CancellationTokenSource? lKeyframeCancelSource;
    private LKeyframeSourceIdentity? lKeyframeSourceIdentity;
    private TimeSpan lKeyframeDuration;
    private int lKeyframeRequestSerial;
    private long lKeyframeNoticeSerial;
    private long lKeyframeNoticeCeiling = -1;
    private bool lKeyframeDisposed;
    private readonly Func<string, TimeSpan, TimeSpan, CancellationToken, IReadOnlyList<LKeyframeEntry>>
        lKeyframeScanner;

    public LKeyframeOrchestrator()
        : this(LKeyframeSeeker.LKeyframeRangeScan)
    {
    }

    internal LKeyframeOrchestrator(
        Func<string, TimeSpan, TimeSpan, CancellationToken, IReadOnlyList<LKeyframeEntry>> scanner)
    {
        lKeyframeScanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    public event Action<LKeyframeNotice>? LKeyframeNoticeReady;

    public int LKeyframeCurrentSerial => lKeyframeRequestSerial;

    public static TimeSpan LKeyframeSearchDuration =>
        LKeyframeView.LKeyframeRangeBefore + LKeyframeView.LKeyframeRangeAfter;

    public void LKeyframeStart(string sourcePath, TimeSpan duration, TimeSpan cursor)
    {
        if (lKeyframeDisposed || string.IsNullOrWhiteSpace(sourcePath) || duration <= TimeSpan.Zero)
        {
            return;
        }

        CancellationTokenSource cancel;
        int serial;
        LKeyframeSourceIdentity? identity;
        lock (lKeyframeLock)
        {
            lKeyframeCancelSource?.Cancel();
            lKeyframeCancelSource?.Dispose();
            lKeyframeCancelSource = new CancellationTokenSource();
            cancel = lKeyframeCancelSource;
            serial = ++lKeyframeRequestSerial;

            identity = lKeyframeSourceIdentity;
            if (LKeyframeSourceCheck(sourcePath, duration))
            {
                identity = null;
                LKeyframeStateClear();
                lKeyframeDuration = duration;
            }
        }

        identity ??= LKeyframeIdentityLoad(sourcePath, duration, serial);
        LKeyframeNoticePublish(serial);
        if (identity is not null)
        {
            LKeyframePlanStart(identity.LKeyframeSourcePath, duration, cursor, serial, cancel.Token);
        }
    }

    public void LKeyframeSuspend()
    {
        CancellationTokenSource? lKeyframeCancelPrevious;
        lock (lKeyframeLock)
        {
            if (lKeyframeDisposed)
            {
                return;
            }

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
        if (previous.LKeyframeFailed || next.LKeyframeFailed)
        {
            return LKeyframeMoveResult.LKeyframeFailedCreate();
        }

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
                direction,
                LKeyframeFailureRead());
        }
    }

    private HashSet<int> LKeyframeFailureRead() =>
        lKeyframeFailedCounts
            .Where(pair => pair.Value >= LKeyframeRetryLimit)
            .Select(pair => pair.Key)
            .ToHashSet();

    internal static LKeyframeMoveResult LKeyframeMoveResolve(
        IReadOnlyCollection<long> keyframes,
        IReadOnlySet<int> scannedSpans,
        TimeSpan duration,
        TimeSpan cursor,
        int direction,
        IReadOnlySet<int>? failedSpans = null)
    {
        long durationMs = Math.Max(0, (long)Math.Ceiling(duration.TotalMilliseconds));
        long cursorMs = Math.Clamp((long)Math.Round(cursor.TotalMilliseconds), 0, durationMs);
        long searchRangeMs = (long)(direction < 0
            ? LKeyframeView.LKeyframeRangeBefore
            : LKeyframeView.LKeyframeRangeAfter).TotalMilliseconds;
        long rangeStartMs = direction < 0 ? Math.Max(0, cursorMs - searchRangeMs) : cursorMs;
        long rangeEndMs = direction < 0 ? cursorMs : Math.Min(durationMs, cursorMs + searchRangeMs);

        if (rangeEndMs <= rangeStartMs)
        {
            return LKeyframeMoveResult.LKeyframeReadyCreate(null);
        }

        long? target = direction < 0
            ? keyframes.Where(ms => ms >= rangeStartMs && ms < cursorMs).Select(ms => (long?)ms).Max()
            : keyframes.Where(ms => ms > cursorMs && ms < rangeEndMs).Select(ms => (long?)ms).Min();

        long coverageStartMs = direction < 0 ? target ?? rangeStartMs : cursorMs;
        long coverageEndMs = direction < 0 ? cursorMs : target is null ? rangeEndMs : target.Value + 1;
        int firstSpan = (int)(coverageStartMs / LKeyframeGridMilliseconds);
        int lastSpan = (int)((coverageEndMs - 1) / LKeyframeGridMilliseconds);
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
}

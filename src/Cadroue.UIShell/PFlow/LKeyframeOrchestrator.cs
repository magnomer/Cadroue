using Cadroue.Media;
using Cadroue.UIShell;

using Cadroue.Core;

namespace Cadroue.UIShell.PFlow;

public sealed partial class LKeyframeOrchestrator : IDisposable
{
    private const int LKeyframeGridMilliseconds = 20000;
    private static readonly TimeSpan lKeyframeRangeBefore = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan lKeyframeRangeAfter = TimeSpan.FromMinutes(10);
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

    public static TimeSpan LKeyframeRangeBefore => lKeyframeRangeBefore;

    public static TimeSpan LKeyframeRangeAfter => lKeyframeRangeAfter;

    public static TimeSpan LKeyframeSearchDuration => lKeyframeRangeBefore + lKeyframeRangeAfter;

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

    public TimeSpan? LKeyframePreviousMove(TimeSpan cursor)
        => LKeyframeMoveFind(cursor, -1);

    public TimeSpan? LKeyframeNextMove(TimeSpan cursor)
        => LKeyframeMoveFind(cursor, 1);

    public TimeSpan? LKeyframeNearestMove(TimeSpan cursor)
    {
        var previous = LKeyframePreviousMove(cursor);
        var next = LKeyframeNextMove(cursor);
        if (previous is null) return next;
        if (next is null) return previous;
        return cursor - previous.Value <= next.Value - cursor ? previous : next;
    }

    private TimeSpan? LKeyframeMoveFind(TimeSpan cursor, int direction)
    {
        long cursorMs = (long)Math.Round(cursor.TotalMilliseconds);
        long? target = null;
        lock (lKeyframeLock)
        {
            foreach (long ms in lKeyframeStorage)
            {
                if (direction < 0)
                {
                    if (ms >= cursorMs) break;
                    target = ms;
                    continue;
                }

                if (ms > cursorMs)
                {
                    target = ms;
                    break;
                }
            }
        }

        return target is null ? null : TimeSpan.FromMilliseconds(target.Value);
    }

}

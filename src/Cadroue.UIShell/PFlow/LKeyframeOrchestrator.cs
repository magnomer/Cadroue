using Cadroue.Media;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PFlow;

public sealed partial class LKeyframeOrchestrator : IDisposable
{
    private const int LKeyframeGridMilliseconds = 20000;
    private static readonly TimeSpan lKeyframeRangeBefore = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan lKeyframeRangeAfter = TimeSpan.FromMinutes(10);
    private readonly object lKeyframeLock = new();
    private readonly SortedSet<long> lKeyframeStorage = new();
    private readonly HashSet<int> lKeyframeScannedSpans = new();
    private const int LKeyframeFailedSpanRetryLimit = 3;
    private readonly Dictionary<int, int> lKeyframeFailedSpanCounts = new();
    private CancellationTokenSource? lKeyframeCancel;
    private LKeyframeSourceIdentity? lKeyframeSourceIdentity;
    private string? lKeyframeSourcePath;
    private TimeSpan lKeyframeDuration;
    private int lKeyframeRequestSerial;
    private bool lDisposed;

    public event Action<LKeyframeNotice>? LKeyframeNoticeReady;

    public int LKeyframeCurrentSerial => lKeyframeRequestSerial;

    public static TimeSpan LKeyframeRangeBefore => lKeyframeRangeBefore;

    public static TimeSpan LKeyframeRangeAfter => lKeyframeRangeAfter;

    public static TimeSpan LKeyframeSearchDuration => lKeyframeRangeBefore + lKeyframeRangeAfter;


    public void LKeyframeStart(string sourcePath, TimeSpan duration, TimeSpan cursor)
    {
        if (lDisposed || string.IsNullOrWhiteSpace(sourcePath) || duration <= TimeSpan.Zero)
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
                lKeyframeFailedSpanCounts.Clear();
                lKeyframeSourceIdentity = identity;
                lKeyframeSourcePath = identity.LKeyframeSourcePath;
                lKeyframeDuration = duration;
                LKeyframeCacheLoad(identity);
            }
            else
            {
                identity = lKeyframeSourceIdentity!;
            }

            lKeyframeCancel?.Cancel();
            lKeyframeCancel?.Dispose();
            lKeyframeCancel = new CancellationTokenSource();
            cancel = lKeyframeCancel;
            serial = ++lKeyframeRequestSerial;
        }

        LKeyframeNoticePublish(serial);
        LKeyframePlanStart(identity.LKeyframeSourcePath, duration, cursor, serial, cancel.Token);
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
        long limit = (long)Math.Round(App.LPreferenceStateCurrent.LPreferenceImmediateKeyframeWindowMilliseconds);
        lock (lKeyframeLock)
        {
            IEnumerable<long> query = direction < 0
                ? lKeyframeStorage.Where(ms => ms < cursorMs).Reverse()
                : lKeyframeStorage.Where(ms => ms > cursorMs);
            foreach (long ms in query)
            {
                if (Math.Abs(ms - cursorMs) > limit) return null;
                return TimeSpan.FromMilliseconds(ms);
            }
        }
        return null;
    }

}
